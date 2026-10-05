using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TranscriptAI.Core;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Options;
using TranscriptAI.Core.Pipeline;
using TranscriptAI.Core.Sources;
using TranscriptAI.Core.Text;
using TranscriptAI.Core.YouTube;

namespace TranscriptAI.Api;

internal static class Endpoints
{
    private static readonly string Version =
        typeof(Endpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "1.0.0";

    public static IEndpointRouteBuilder MapTranscriptAIEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", GetHealth)
            .WithName("Health")
            .WithTags("Health")
            .WithSummary("Service status and capabilities (used by the portfolio to adapt the demo UI)");

        var transcriptions = app.MapGroup("/api/transcriptions")
            .WithTags("Transcriptions")
            .WithMetadata(DemoQuotaMetadata.Instance); // rate limited — see DemoRateLimiting

        transcriptions.MapPost("/youtube", TranscribeYouTubeAsync)
            .WithName("TranscribeYouTube")
            .WithSummary("Transcribe the audio of a YouTube video");

        transcriptions.MapPost("/file", TranscribeFileAsync)
            .WithName("TranscribeFile")
            .WithSummary("Transcribe an uploaded audio or video file")
            .DisableAntiforgery(); // Cookie-less JSON API called cross-origin: CSRF tokens don't apply.

        return app;
    }

    internal static HealthResponse GetHealth(
        TranscriptPipeline pipeline,
        IOptions<TranscriptionOptions> transcription,
        IOptions<DemoOptions> demo)
    {
        var provider = pipeline.Provider;
        var settings = demo.Value;
        return new HealthResponse(
            provider.IsConfigured ? "ok" : "degraded",
            Version,
            provider.Name,
            provider.IsConfigured,
            provider.DefaultModel(TranscriptionTask.Transcribe),
            provider.DefaultModel(TranscriptionTask.Translate),
            provider.SupportsTranslation,
            pipeline.CanConvertMedia,
            settings.AllowYouTube,
            Math.Min(settings.MaxUploadMegabytes, transcription.Value.MaxUploadMegabytes),
            settings.MaxDurationMinutes,
            pipeline.CanConvertMedia ? AudioFormats.AllExtensions : AudioFormats.DirectUploadExtensions);
    }

    internal static async Task<IResult> TranscribeYouTubeAsync(
        YouTubeTranscriptionRequest body,
        TranscriptPipeline pipeline,
        IYouTubeAudioDownloader youtube,
        DailyQuota quota,
        IOptions<DemoOptions> demo,
        IOptions<TranscriptionOptions> transcription,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        EnsureProviderConfigured(pipeline);
        var settings = demo.Value;
        if (!settings.AllowYouTube)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "YouTube disabled",
                detail: "YouTube transcription is turned off on this server. Upload an audio file instead.");
        }

        if (RequestValidation.Validate(body.Task, body.Language, out var request) is { } invalid)
        {
            return invalid;
        }

        if (string.IsNullOrWhiteSpace(body.Url) || !MediaSource.TryParseYouTube(body.Url, out var video))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = ["Enter a YouTube video link, e.g. https://www.youtube.com/watch?v=…"],
            });
        }

        var info = await youtube.GetInfoAsync(video.Url, cancellationToken);
        if (info.Duration is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Live stream",
                detail: "Live streams can't be transcribed. Try a regular video.");
        }

        if (info.Duration > TimeSpan.FromMinutes(settings.MaxDurationMinutes))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Video too long",
                detail: $"This video is {info.Duration.Value.TotalMinutes:F0} minutes long; the demo accepts up to {settings.MaxDurationMinutes} minutes. " +
                        "Run the CLI locally for longer recordings.");
        }

        if (!quota.TryConsume(out var resetIn))
        {
            return DailyLimitReached(httpContext, resetIn);
        }

        var workDirectory = CreateWorkDirectory();
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var download = await youtube.DownloadAudioAsync(
                video.Url,
                workDirectory,
                "source",
                preferredMaxBytes: pipeline.CanConvertMedia ? null : transcription.Value.MaxUploadBytes,
                cancellationToken: cancellationToken);

            var result = await pipeline.TranscribeAsync(
                download.Path,
                info.Title,
                request,
                new AudioPreparationRequest(workDirectory, "audio"),
                cancellationToken: cancellationToken);

            return Results.Ok(TranscriptionResponse.From(result, "youtube", stopwatch.ElapsedMilliseconds));
        }
        finally
        {
            TryDeleteDirectory(workDirectory);
        }
    }

    internal static async Task<IResult> TranscribeFileAsync(
        IFormFile? file,
        [FromForm] string? task,
        [FromForm] string? language,
        TranscriptPipeline pipeline,
        DailyQuota quota,
        IOptions<DemoOptions> demo,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["Choose an audio or video file to upload."],
            });
        }

        var settings = demo.Value;
        if (file.Length > settings.MaxUploadBytes)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "File too large",
                detail: $"Uploads are limited to {settings.MaxUploadMegabytes} MB in the demo.");
        }

        var supported = pipeline.CanConvertMedia
            ? AudioFormats.IsKnownMediaFile(file.FileName)
            : AudioFormats.IsDirectlySupported(file.FileName);
        if (!supported)
        {
            var accepted = pipeline.CanConvertMedia ? AudioFormats.AllExtensions : AudioFormats.DirectUploadExtensions;
            return Results.Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType,
                title: "Unsupported file type",
                detail: $"Accepted formats: {string.Join(", ", accepted)}.");
        }

        if (RequestValidation.Validate(task, language, out var request) is { } invalid)
        {
            return invalid;
        }

        EnsureProviderConfigured(pipeline);
        if (!quota.TryConsume(out var resetIn))
        {
            return DailyLimitReached(httpContext, resetIn);
        }

        var workDirectory = CreateWorkDirectory();
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // The client's file name is only used for display and its (allow-listed) extension.
            var inputPath = Path.Combine(workDirectory, "upload" + Path.GetExtension(file.FileName).ToLowerInvariant());
            await using (var target = File.Create(inputPath))
            {
                await file.CopyToAsync(target, cancellationToken);
            }

            var title = Path.GetFileNameWithoutExtension(file.FileName);
            var result = await pipeline.TranscribeAsync(
                inputPath,
                string.IsNullOrWhiteSpace(title) ? "transcript" : FileNameSanitizer.Sanitize(title),
                request,
                new AudioPreparationRequest(workDirectory, "audio"),
                cancellationToken: cancellationToken);

            return Results.Ok(TranscriptionResponse.From(result, "upload", stopwatch.ElapsedMilliseconds));
        }
        finally
        {
            TryDeleteDirectory(workDirectory);
        }
    }

    private static IResult DailyLimitReached(HttpContext httpContext, TimeSpan resetIn)
    {
        DemoRateLimiting.SetRetryAfter(httpContext.Response, resetIn);
        return Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Daily limit reached",
            detail: "The live demo has reached its daily limit. Please come back tomorrow, or run the CLI locally with your own free Groq API key.");
    }

    /// <summary>Fails fast (503) before downloading or saving anything when the API key is missing.</summary>
    private static void EnsureProviderConfigured(TranscriptPipeline pipeline)
    {
        if (!pipeline.Provider.IsConfigured)
        {
            throw new TranscriptionException(
                $"{pipeline.Provider.Name} API key is not configured.",
                TranscriptionErrorKind.Configuration);
        }
    }

    private static string CreateWorkDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "transcriptai", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Temp files are cleaned up by the OS eventually.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
