using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TranscriptAI.Core;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Transcription;
using TranscriptAI.Core.YouTube;

namespace TranscriptAI.Tests.Api;

/// <summary>The real API pipeline with a fake AI provider, a fake YouTube downloader and no FFmpeg.</summary>
internal sealed class ApiFactory(Dictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    public const string PortfolioOrigin = "https://portfolio.example";

    public FakeTranscriptionProvider Provider { get; } = new();

    public FakeYouTubeDownloader YouTube { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Cors:AllowedOrigins:0", PortfolioOrigin + "/"); // trailing slash is normalised away
        builder.UseSetting("Demo:PermitLimitPerWindow", "100");
        foreach (var (key, value) in settings ?? [])
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITranscriptionProvider>();
            services.AddSingleton<ITranscriptionProvider>(Provider);
            services.RemoveAll<IYouTubeAudioDownloader>();
            services.AddSingleton<IYouTubeAudioDownloader>(YouTube);
            services.RemoveAll<IFFmpegLocator>();
            services.AddSingleton<IFFmpegLocator>(new StubFFmpegLocator(null)); // files go to the provider as-is
        });
    }
}

public sealed class ApiTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static MultipartFormDataContent Upload(string fileName, int sizeBytes = 2048, string? task = null, string? language = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[sizeBytes]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        if (task is not null)
        {
            form.Add(new StringContent(task), "task");
        }

        if (language is not null)
        {
            form.Add(new StringContent(language), "language");
        }

        return form;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    private static HttpRequestMessage FromClient(HttpMethod method, string url, string clientIp, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Add("X-Forwarded-For", clientIp);
        return request;
    }

    [Fact]
    public async Task Health_describes_what_the_demo_can_do()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var health = await JsonAsync(await client.GetAsync("/api/health", Ct));

        Assert.Equal("ok", health.GetProperty("status").GetString());
        Assert.Equal("Fake", health.GetProperty("provider").GetString());
        Assert.True(health.GetProperty("providerConfigured").GetBoolean());
        Assert.False(health.GetProperty("ffmpegAvailable").GetBoolean());
        Assert.True(health.GetProperty("youTubeEnabled").GetBoolean());
        Assert.Equal(10, health.GetProperty("maxUploadMegabytes").GetInt32());
        Assert.Equal(15, health.GetProperty("maxDurationMinutes").GetInt32());
        var extensions = health.GetProperty("acceptedExtensions").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(".mp3", extensions);
        Assert.DoesNotContain(".mkv", extensions); // needs FFmpeg
    }

    [Fact]
    public async Task Health_is_degraded_without_an_api_key()
    {
        await using var factory = new ApiFactory();
        factory.Provider.IsConfigured = false;
        using var client = factory.CreateClient();

        var health = await JsonAsync(await client.GetAsync("/api/health", Ct));

        Assert.Equal("degraded", health.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Transcribes_an_uploaded_file_and_cleans_up()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/transcriptions/file", Upload("My: talk?.MP3", language: "EN"), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal("My talk", body.GetProperty("title").GetString());
        Assert.Equal("My talk", body.GetProperty("fileName").GetString());
        Assert.Equal("upload", body.GetProperty("source").GetString());
        Assert.Equal("transcribe", body.GetProperty("task").GetString());
        Assert.Equal("Hello world", body.GetProperty("text").GetString());
        Assert.Equal("[0.00 --> 1.50] Hello world\n", body.GetProperty("documents").GetProperty("txt").GetString());
        Assert.StartsWith("WEBVTT", body.GetProperty("documents").GetProperty("vtt").GetString());
        var segment = Assert.Single(body.GetProperty("segments").EnumerateArray().ToList());
        Assert.Equal(1.5, segment.GetProperty("end").GetDouble());

        var call = Assert.Single(factory.Provider.Calls);
        Assert.True(call.AudioExisted);
        Assert.Equal("upload.mp3", Path.GetFileName(call.AudioPath)); // client file names never reach the disk
        Assert.Equal("en", call.Request.Language);
        Assert.False(File.Exists(call.AudioPath)); // work folder deleted after the request
    }

    [Fact]
    public async Task Missing_file_is_a_validation_error()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent { { new StringContent("transcribe"), "task" } };

        using var response = await client.PostAsync("/api/transcriptions/file", form, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty("file", out _));
    }

    [Theory]
    [InlineData("summarize", null, "task")]
    [InlineData(null, "english", "language")]
    public async Task Invalid_options_are_validation_errors(string? task, string? language, string field)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/transcriptions/file", Upload("a.mp3", task: task, language: language), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty(field, out _));
        Assert.Empty(factory.Provider.Calls);
    }

    [Fact]
    public async Task Unsupported_file_types_are_rejected()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/transcriptions/file", Upload("movie.mkv"), Ct);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Contains(".mp3", (await JsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Files_over_the_demo_limit_are_rejected()
    {
        await using var factory = new ApiFactory(new() { ["Demo:MaxUploadMegabytes"] = "1" });
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/transcriptions/file", Upload("long.mp3", sizeBytes: 1_200_000), Ct);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(factory.Provider.Calls);
    }

    [Fact]
    public async Task Transcribes_and_translates_a_youtube_video()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/transcriptions/youtube",
            new { url = "https://youtu.be/dQw4w9WgXcQ?si=share", task = "translate" },
            Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await JsonAsync(response);
        Assert.Equal("Test: a video?", body.GetProperty("title").GetString());
        Assert.Equal("Test a video", body.GetProperty("fileName").GetString());
        Assert.Equal("youtube", body.GetProperty("source").GetString());
        Assert.Equal("translate", body.GetProperty("task").GetString());
        Assert.Equal(TranscriptionTask.Translate, Assert.Single(factory.Provider.Calls).Request.Task);
    }

    [Fact]
    public async Task Invalid_youtube_links_are_validation_errors()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/transcriptions/youtube", new { url = "https://vimeo.com/123" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await JsonAsync(response)).GetProperty("errors").TryGetProperty("url", out _));
    }

    [Theory]
    [InlineData(45, "Video too long")]
    [InlineData(null, "Live stream")]
    public async Task Long_videos_and_live_streams_are_refused_before_downloading(int? minutes, string title)
    {
        await using var factory = new ApiFactory();
        factory.YouTube.Info = factory.YouTube.Info with { Duration = minutes is { } m ? TimeSpan.FromMinutes(m) : null };
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/transcriptions/youtube", new { url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ" }, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(title, (await JsonAsync(response)).GetProperty("title").GetString());
        Assert.Equal(0, factory.YouTube.Downloads);
    }

    [Fact]
    public async Task YouTube_can_be_switched_off()
    {
        await using var factory = new ApiFactory(new() { ["Demo:AllowYouTube"] = "false" });
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/transcriptions/youtube", new { url = "https://youtu.be/dQw4w9WgXcQ" }, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unconfigured_provider_fails_fast_without_leaking_details()
    {
        await using var factory = new ApiFactory();
        factory.Provider.IsConfigured = false;
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/transcriptions/youtube", new { url = "https://youtu.be/dQw4w9WgXcQ" }, Ct);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("API key", (await JsonAsync(response)).GetProperty("detail").GetString());
        Assert.Equal(0, factory.YouTube.Downloads);
    }

    [Fact]
    public async Task Provider_rate_limits_become_429_with_retry_after()
    {
        await using var factory = new ApiFactory();
        factory.Provider.Exception = new TranscriptionException("Groq rate limit reached", TranscriptionErrorKind.RateLimited)
        {
            RetryAfter = TimeSpan.FromSeconds(29.2),
        };
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/transcriptions/file", Upload("a.wav"), Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.RetryAfter?.Delta);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task YouTube_failures_are_explained_to_the_visitor()
    {
        await using var factory = new ApiFactory();
        factory.YouTube.Exception = new TranscriptionException("This video is unavailable.", TranscriptionErrorKind.MediaUnavailable);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/transcriptions/youtube", new { url = "https://youtu.be/dQw4w9WgXcQ" }, Ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("This video is unavailable.", (await JsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Each_visitor_gets_a_small_number_of_requests_per_window()
    {
        await using var factory = new ApiFactory(new() { ["Demo:PermitLimitPerWindow"] = "2" });
        using var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            using var ok = await client.SendAsync(FromClient(HttpMethod.Post, "/api/transcriptions/file", "203.0.113.7", Upload("a.mp3")), Ct);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        using var limited = await client.SendAsync(FromClient(HttpMethod.Post, "/api/transcriptions/file", "203.0.113.7", Upload("a.mp3")), Ct);
        using var otherVisitor = await client.SendAsync(FromClient(HttpMethod.Post, "/api/transcriptions/file", "198.51.100.9", Upload("a.mp3")), Ct);
        using var health = await client.SendAsync(FromClient(HttpMethod.Get, "/api/health", "203.0.113.7"), Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter?.Delta);
        Assert.Contains("several requests", (await JsonAsync(limited)).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.OK, otherVisitor.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode); // only the AI endpoints are limited
    }

    [Fact]
    public async Task Only_started_jobs_count_toward_the_daily_allowance()
    {
        await using var factory = new ApiFactory(new()
        {
            ["Demo:PermitLimitPerWindow"] = "1",
            ["Demo:DailyLimit"] = "2",
        });
        using var client = factory.CreateClient();

        async Task<HttpStatusCode> Post(string ip, string? task = null)
        {
            using var response = await client.SendAsync(
                FromClient(HttpMethod.Post, "/api/transcriptions/file", ip, Upload("a.mp3", task: task)), Ct);
            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.OK, await Post("203.0.113.1"));                   // job 1 of 2
        Assert.Equal(HttpStatusCode.TooManyRequests, await Post("203.0.113.1"));      // per-visitor limit, no job
        Assert.Equal(HttpStatusCode.BadRequest, await Post("203.0.113.2", "dance")); // invalid, no job
        Assert.Equal(HttpStatusCode.OK, await Post("203.0.113.3"));                   // job 2 of 2

        using var daily = await client.SendAsync(FromClient(HttpMethod.Post, "/api/transcriptions/file", "203.0.113.4", Upload("a.mp3")), Ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, daily.StatusCode);
        Assert.Equal("Daily limit reached", (await JsonAsync(daily)).GetProperty("title").GetString());
        Assert.InRange(daily.Headers.RetryAfter!.Delta!.Value, TimeSpan.FromSeconds(1), TimeSpan.FromDays(1));
        Assert.Equal(2, factory.Provider.Calls.Count);
    }

    [Fact]
    public async Task Concurrent_jobs_are_capped()
    {
        await using var factory = new ApiFactory(new() { ["Demo:MaxConcurrentJobs"] = "1" });
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Provider.Gate = () =>
        {
            started.TrySetResult();
            return release.Task;
        };
        using var client = factory.CreateClient();

        var running = client.PostAsync("/api/transcriptions/file", Upload("a.mp3"), Ct); // holds the only slot
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        var queued = client.PostAsync("/api/transcriptions/file", Upload("b.mp3"), Ct);  // waits in the queue (size 1)
        await Task.Delay(200, Ct);

        using var rejected = await client.PostAsync("/api/transcriptions/file", Upload("c.mp3"), Ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Contains("busy", (await JsonAsync(rejected)).GetProperty("detail").GetString());

        release.SetResult();
        using var first = await running;
        using var second = await queued;
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Cors_allows_only_the_portfolio_origin()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        static HttpRequestMessage Preflight(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/transcriptions/youtube");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type");
            return request;
        }

        using var allowed = await client.SendAsync(Preflight(ApiFactory.PortfolioOrigin), Ct);
        using var blocked = await client.SendAsync(Preflight("https://evil.example"), Ct);

        Assert.Equal(ApiFactory.PortfolioOrigin, Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(blocked.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Error_responses_stay_readable_cross_origin()
    {
        await using var factory = new ApiFactory();
        factory.Provider.Exception = new TranscriptionException("busy", TranscriptionErrorKind.RateLimited) { RetryAfter = TimeSpan.FromSeconds(5) };
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transcriptions/file") { Content = Upload("a.mp3") };
        request.Headers.Add("Origin", ApiFactory.PortfolioOrigin);

        using var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(ApiFactory.PortfolioOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("Retry-After", response.Headers.GetValues("Access-Control-Expose-Headers").Single());
    }

    [Fact]
    public async Task Root_and_openapi_describe_the_service()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var root = await JsonAsync(await client.GetAsync("/", Ct));
        var openApi = await client.GetStringAsync("/openapi/v1.json", Ct);

        Assert.Equal("/api/health", root.GetProperty("health").GetString());
        Assert.Contains("/api/transcriptions/file", openApi);
        Assert.Contains("/api/transcriptions/youtube", openApi);
    }
}
