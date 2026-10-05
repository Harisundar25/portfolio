using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using TranscriptAI.Api;
using TranscriptAI.Core;
using TranscriptAI.Core.Options;

var builder = WebApplication.CreateBuilder(args);

// The transcription pipeline shared with the CLI, plus the public demo's guard rails.
builder.Services.AddTranscriptAI(builder.Configuration);
builder.Services.AddOptions<TranscriptionOptions>().ValidateOnStart();
builder.Services.AddOptions<DemoOptions>()
    .Bind(builder.Configuration.GetSection(DemoOptions.SectionName))
    .Validate(o => o.MaxUploadMegabytes is > 0 and <= 100, "Demo:MaxUploadMegabytes must be between 1 and 100.")
    .Validate(o => o.MaxDurationMinutes > 0, "Demo:MaxDurationMinutes must be positive.")
    .Validate(o => o.PermitLimitPerWindow > 0 && o.WindowMinutes > 0, "Demo:PermitLimitPerWindow and Demo:WindowMinutes must be positive.")
    .Validate(o => o.DailyLimit > 0 && o.MaxConcurrentJobs > 0, "Demo:DailyLimit and Demo:MaxConcurrentJobs must be positive.")
    .ValidateOnStart();

// Accept uploads up to the demo limit (plus multipart framing) and nothing larger.
builder.Services.AddOptions<KestrelServerOptions>()
    .Configure<IOptions<DemoOptions>>((kestrel, demo) => kestrel.Limits.MaxRequestBodySize = demo.Value.MaxRequestBytes);
builder.Services.AddOptions<FormOptions>()
    .Configure<IOptions<DemoOptions>>((form, demo) => form.MultipartBodyLengthLimit = demo.Value.MaxRequestBytes);

// Azure App Service terminates TLS in front of the app and appends the visitor's address to
// X-Forwarded-For. Its front ends have no fixed IPs, so trust the single hop in front of the app.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// The portfolio (Azure Static Web Apps) calls this API cross-origin; list its origin(s) in Cors:AllowedOrigins.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(AllowedOrigins(builder.Configuration))
    .WithMethods("GET", "POST")
    .WithHeaders("Content-Type")
    .WithExposedHeaders("Retry-After")
    .SetPreflightMaxAge(TimeSpan.FromHours(1))));

builder.Services.AddDemoRateLimiting();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TranscriptionExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true makes the host process these headers itself;
// running the middleware twice would trust one hop too many.
if (!string.Equals(app.Configuration["FORWARDEDHEADERS_ENABLED"], "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

app.MapOpenApi();
app.MapGet("/", () => TypedResults.Ok(new
    {
        Name = "TranscriptAI API",
        Description = "C# / .NET port of the Transcript-AI script: YouTube or uploaded media → timestamped transcript via open-source Whisper models.",
        Health = "/api/health",
        OpenApi = "/openapi/v1.json",
        Source = "https://github.com/Harisundar25/portfolio/tree/main/backend",
    }))
    .ExcludeFromDescription();
app.MapTranscriptAIEndpoints();

app.Run();

/// <summary>Origins without trailing slashes — CORS compares scheme://host[:port] exactly.</summary>
static string[] AllowedOrigins(IConfiguration configuration) =>
    (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Where(origin => origin.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
