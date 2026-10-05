using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TranscriptAI.Core;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Options;
using TranscriptAI.Core.Pipeline;
using TranscriptAI.Core.Transcription;

namespace TranscriptAI.Tests.Core;

public sealed class HttpRetryTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static RetryOptions Fast(int maxRetries = 2, double maxDelaySeconds = 5) =>
        new() { MaxRetries = maxRetries, BaseDelaySeconds = 0, MaxDelaySeconds = maxDelaySeconds };

    private static Task<HttpResponseMessage> SendAsync(FakeHttpHandler handler, RetryOptions retry, Action? onBuild = null) =>
        HttpRetry.SendAsync(
            handler.CreateClient("https://api.test/"),
            () =>
            {
                onBuild?.Invoke();
                return new HttpRequestMessage(HttpMethod.Post, "audio") { Content = new StringContent("payload") };
            },
            retry,
            NullLogger.Instance,
            "Test",
            Ct);

    [Fact]
    public async Task Retries_transient_failures_with_a_fresh_request_each_time()
    {
        var handler = new FakeHttpHandler()
            .RespondStatus(HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.Zero)
            .RespondStatus(HttpStatusCode.ServiceUnavailable)
            .RespondJson("{}");
        var built = 0;

        using var response = await SendAsync(handler, Fast(), () => built++);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, built);
        Assert.All(handler.Requests, r => Assert.Equal("payload", r.Body));
    }

    [Fact]
    public async Task Returns_the_last_response_after_max_retries()
    {
        var handler = new FakeHttpHandler().RespondStatus(HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.Zero);

        using var response = await SendAsync(handler, Fast(maxRetries: 2));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Gives_up_immediately_when_asked_to_wait_too_long()
    {
        var handler = new FakeHttpHandler().RespondStatus(HttpStatusCode.TooManyRequests, retryAfter: TimeSpan.FromMinutes(10));

        using var response = await SendAsync(handler, Fast(maxDelaySeconds: 5));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Does_not_retry_client_errors()
    {
        var handler = new FakeHttpHandler().RespondStatus(HttpStatusCode.BadRequest);

        using var response = await SendAsync(handler, Fast());

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Network_errors_are_retried_then_reported()
    {
        var handler = new FakeHttpHandler().Throw(new HttpRequestException("connection refused"));

        var error = await Assert.ThrowsAsync<TranscriptionException>(() => SendAsync(handler, Fast(maxRetries: 1)));

        Assert.Equal(TranscriptionErrorKind.Upstream, error.Kind);
        Assert.Contains("Couldn't reach Test", error.Message);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Http_client_timeouts_are_reported_as_upstream_errors()
    {
        var handler = new FakeHttpHandler().Throw(new TaskCanceledException("timeout"));

        var error = await Assert.ThrowsAsync<TranscriptionException>(() => SendAsync(handler, Fast()));

        Assert.Equal(TranscriptionErrorKind.Upstream, error.Kind);
        Assert.Contains("did not respond in time", error.Message);
    }

    [Fact]
    public void Reads_retry_after_as_a_date()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(30));

        var wait = HttpRetry.GetRetryAfter(response);

        Assert.NotNull(wait);
        Assert.InRange(wait.Value.TotalSeconds, 25, 31);
    }

    [Fact]
    public void Backoff_doubles_on_each_attempt()
    {
        var retry = new RetryOptions { BaseDelaySeconds = 2 };

        Assert.Equal(new[] { 2d, 4d, 8d }, Enumerable.Range(0, 3).Select(a => HttpRetry.Backoff(retry, a).TotalSeconds));
    }
}

public sealed class ProviderErrorsTests
{
    [Theory]
    [InlineData("""{"error":{"message":"Invalid API Key","type":"invalid_request_error"}}""", "Invalid API Key")]
    [InlineData("""{"error":"Model openai/whisper-large-v3 is currently loading"}""", "Model openai/whisper-large-v3 is currently loading")]
    [InlineData("""{"error":["first","second"]}""", "first; second")]
    [InlineData("Bad gateway", "Bad gateway")]
    public void Extracts_the_message_from_provider_error_bodies(string body, string expected)
    {
        Assert.Equal(expected, ProviderErrors.ExtractMessage(body));
    }

    [Fact]
    public void Truncates_long_non_json_bodies()
    {
        var message = ProviderErrors.ExtractMessage(new string('x', 1000));

        Assert.Equal(301, message!.Length);
        Assert.EndsWith("…", message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, TranscriptionErrorKind.Configuration)]
    [InlineData(HttpStatusCode.PaymentRequired, TranscriptionErrorKind.Configuration)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, TranscriptionErrorKind.TooLarge)]
    [InlineData(HttpStatusCode.TooManyRequests, TranscriptionErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.BadRequest, TranscriptionErrorKind.InvalidInput)]
    [InlineData(HttpStatusCode.BadGateway, TranscriptionErrorKind.Upstream)]
    public async Task Maps_status_codes_to_error_kinds(HttpStatusCode status, TranscriptionErrorKind expected)
    {
        using var response = new HttpResponseMessage(status) { Content = new StringContent("""{"error":{"message":"nope"}}""") };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(12));

        var error = await ProviderErrors.FromResponseAsync(response, "Groq", TestContext.Current.CancellationToken);

        Assert.Equal(expected, error.Kind);
        Assert.Contains("nope", error.Message);
        Assert.Equal(TimeSpan.FromSeconds(12), error.RetryAfter);
    }
}

public sealed class GroqTranscriptionProviderTests
{
    private const string VerboseJson = """
        {
          "task": "transcribe", "language": "tamil", "duration": 4.2, "text": " Vanakkam. Hello there. ",
          "segments": [
            { "id": 0, "start": 0.0, "end": 2.0, "text": " Vanakkam." },
            { "id": 1, "start": 2.0, "end": 4.2, "text": " Hello there. " },
            { "id": 2, "start": 4.2, "end": 4.2, "text": "   " }
          ]
        }
        """;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static GroqTranscriptionProvider Create(FakeHttpHandler handler, Action<TranscriptionOptions>? configure = null) =>
        new(handler.CreateClient("https://api.groq.test/openai/v1/"), TestOptions.Create(configure), NullLogger<GroqTranscriptionProvider>.Instance);

    [Fact]
    public async Task Transcribes_with_timestamped_segments()
    {
        using var temp = new TempDirectory();
        var audio = temp.CreateFile("clip.mp3", 64);
        var handler = new FakeHttpHandler().RespondJson(VerboseJson);

        var result = await Create(handler).TranscribeAsync(
            audio,
            new TranscriptionRequest { Language = "TA", Prompt = "Tamil and English" },
            Ct);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.groq.test/openai/v1/audio/transcriptions", request.Uri!.ToString());
        Assert.Equal("Bearer test-groq-key", request.Authorization);
        Assert.Equal("whisper-large-v3-turbo", request.Fields["model"]);
        Assert.Equal("verbose_json", request.Fields["response_format"]);
        Assert.Equal("segment", request.Fields["timestamp_granularities[]"]);
        Assert.Equal("ta", request.Fields["language"]);
        Assert.Equal("Tamil and English", request.Fields["prompt"]);
        var file = request.Files["file"];
        Assert.Equal(("audio.mp3", "audio/mpeg", 64), (file.FileName, file.ContentType, file.Content.Length));

        Assert.Equal(new[] { new TranscriptSegment(0, 2, "Vanakkam."), new TranscriptSegment(2, 4.2, "Hello there.") }, result.Segments);
        Assert.Equal("Vanakkam. Hello there.", result.Text);
        Assert.Equal("tamil", result.Language);
        Assert.Equal(4.2, result.DurationSeconds);
        Assert.Equal("whisper-large-v3-turbo", result.Model);
    }

    [Fact]
    public async Task Translation_uses_the_translations_endpoint_and_large_v3()
    {
        using var temp = new TempDirectory();
        var audio = temp.CreateFile("clip.m4a");
        var handler = new FakeHttpHandler().RespondJson(VerboseJson);

        var result = await Create(handler).TranscribeAsync(
            audio,
            new TranscriptionRequest { Task = TranscriptionTask.Translate, Language = "ta" },
            Ct);

        var request = Assert.Single(handler.Requests);
        Assert.EndsWith("/audio/translations", request.Uri!.AbsolutePath);
        Assert.Equal("whisper-large-v3", request.Fields["model"]);
        Assert.False(request.Fields.ContainsKey("language"));
        Assert.False(request.Fields.ContainsKey("timestamp_granularities[]"));
        Assert.Equal("audio/mp4", request.Files["file"].ContentType);
        Assert.Equal("English", result.Language);
    }

    [Fact]
    public async Task A_model_override_is_sent_as_is()
    {
        using var temp = new TempDirectory();
        var handler = new FakeHttpHandler().RespondJson(VerboseJson);

        var result = await Create(handler).TranscribeAsync(
            temp.CreateFile("clip.wav"),
            new TranscriptionRequest { Model = "whisper-large-v3" },
            Ct);

        Assert.Equal("whisper-large-v3", handler.Requests[0].Fields["model"]);
        Assert.Equal("whisper-large-v3", result.Model);
    }

    [Fact]
    public async Task Keeps_text_when_the_response_has_no_segments()
    {
        using var temp = new TempDirectory();
        var handler = new FakeHttpHandler().RespondJson("""{ "text": " Hi there ", "duration": 1.25 }""");

        var result = await Create(handler).TranscribeAsync(temp.CreateFile("clip.mp3"), new TranscriptionRequest(), Ct);

        Assert.Equal(new[] { new TranscriptSegment(0, 1.25, "Hi there") }, result.Segments);
    }

    [Fact]
    public async Task Missing_api_key_fails_before_calling_groq()
    {
        using var temp = new TempDirectory();
        var handler = new FakeHttpHandler();

        var error = await Assert.ThrowsAsync<TranscriptionException>(() =>
            Create(handler, o => o.Groq.ApiKey = null).TranscribeAsync(temp.CreateFile("clip.mp3"), new TranscriptionRequest(), Ct));

        Assert.Equal(TranscriptionErrorKind.Configuration, error.Kind);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Rejected_keys_are_configuration_errors()
    {
        using var temp = new TempDirectory();
        var handler = new FakeHttpHandler().RespondJson("""{"error":{"message":"Invalid API Key"}}""", HttpStatusCode.Unauthorized);

        var error = await Assert.ThrowsAsync<TranscriptionException>(() =>
            Create(handler).TranscribeAsync(temp.CreateFile("clip.mp3"), new TranscriptionRequest(), Ct));

        Assert.Equal(TranscriptionErrorKind.Configuration, error.Kind);
        Assert.Equal("Groq rejected the request (401): Invalid API Key. Check the API key / account configuration.", error.Message);
    }

    [Fact]
    public async Task Persistent_rate_limits_surface_retry_after()
    {
        using var temp = new TempDirectory();
        var handler = new FakeHttpHandler().RespondStatus(
            HttpStatusCode.TooManyRequests,
            retryAfter: TimeSpan.FromMinutes(2),
            body: """{"error":{"message":"Rate limit reached for model whisper-large-v3-turbo"}}""");

        var error = await Assert.ThrowsAsync<TranscriptionException>(() =>
            Create(handler).TranscribeAsync(temp.CreateFile("clip.mp3"), new TranscriptionRequest(), Ct));

        Assert.Equal(TranscriptionErrorKind.RateLimited, error.Kind);
        Assert.Equal(TimeSpan.FromMinutes(2), error.RetryAfter);
        Assert.Single(handler.Requests); // 2 minutes is longer than MaxDelaySeconds, so no retry
    }
}

public sealed class HuggingFaceTranscriptionProviderTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static HuggingFaceTranscriptionProvider Create(FakeHttpHandler handler, Action<TranscriptionOptions>? configure = null) =>
        new(
            handler.CreateClient("https://router.hf.test/hf-inference/models/"),
            TestOptions.Create(configure),
            NullLogger<HuggingFaceTranscriptionProvider>.Instance);

    [Fact]
    public async Task Sends_base64_audio_and_reads_timestamped_chunks()
    {
        using var temp = new TempDirectory();
        var audio = temp.CreateFile("clip.flac", 32);
        var handler = new FakeHttpHandler().RespondJson("""
            {
              "text": " Hello world. Second part.",
              "chunks": [
                { "text": " Hello world.", "timestamp": [0.0, 1.8] },
                { "text": " Second part.", "timestamp": [1.8, null] },
                { "text": " ", "timestamp": [3.0, 3.5] }
              ]
            }
            """);

        var result = await Create(handler).TranscribeAsync(audio, new TranscriptionRequest(), Ct);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://router.hf.test/hf-inference/models/openai/whisper-large-v3", request.Uri!.ToString());
        Assert.Equal("Bearer test-hf-token", request.Authorization);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(Convert.ToBase64String(new byte[32]), body.RootElement.GetProperty("inputs").GetString());
        Assert.True(body.RootElement.GetProperty("parameters").GetProperty("return_timestamps").GetBoolean());

        Assert.Equal(new[] { new TranscriptSegment(0, 1.8, "Hello world."), new TranscriptSegment(1.8, 1.8, "Second part.") }, result.Segments);
        Assert.Equal("Hello world. Second part.", result.Text);
        Assert.Equal(1.8, result.DurationSeconds);
        Assert.Equal("openai/whisper-large-v3", result.Model);
    }

    [Fact]
    public async Task Translation_is_not_supported()
    {
        using var temp = new TempDirectory();
        var handler = new FakeHttpHandler();

        var error = await Assert.ThrowsAsync<TranscriptionException>(() =>
            Create(handler).TranscribeAsync(temp.CreateFile("clip.mp3"), new TranscriptionRequest { Task = TranscriptionTask.Translate }, Ct));

        Assert.Equal(TranscriptionErrorKind.NotSupported, error.Kind);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Missing_token_is_a_configuration_error()
    {
        using var temp = new TempDirectory();

        var error = await Assert.ThrowsAsync<TranscriptionException>(() =>
            Create(new FakeHttpHandler(), o => o.HuggingFace.ApiKey = " ").TranscribeAsync(temp.CreateFile("clip.mp3"), new TranscriptionRequest(), Ct));

        Assert.Equal(TranscriptionErrorKind.Configuration, error.Kind);
    }
}

public sealed class TranscriptPipelineTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed class ImmediateProgress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }

    [Fact]
    public async Task Stitches_chunks_back_onto_the_original_timeline()
    {
        var preparer = new FakeAudioPreparer(new AudioChunk("a.mp3", 0, 600), new AudioChunk("b.mp3", 600, 120))
        {
            TotalDurationSeconds = 720,
            ConvertedPath = "full.mp3",
        };
        var provider = new FakeTranscriptionProvider
        {
            OnTranscribe = (path, _) => path == "a.mp3"
                ? new ProviderTranscription("First part.", [new TranscriptSegment(0, 4, "First part.")], "english", 600, "m1")
                : new ProviderTranscription("Second part.", [new TranscriptSegment(1, 3, "Second part.")], null, 120, "m1"),
        };
        var messages = new List<string>();
        var pipeline = new TranscriptPipeline(preparer, provider, NullLogger<TranscriptPipeline>.Instance);

        var result = await pipeline.TranscribeAsync(
            "input.mp4",
            "My talk",
            new TranscriptionRequest(),
            new AudioPreparationRequest("work", "audio"),
            new ImmediateProgress(messages),
            Ct);

        Assert.Equal(new[] { new TranscriptSegment(0, 4, "First part."), new TranscriptSegment(601, 603, "Second part.") }, result.Segments);
        Assert.Equal("First part. Second part.", result.Text);
        Assert.Equal(2, result.ChunkCount);
        Assert.Equal(720, result.DurationSeconds);
        Assert.Equal("english", result.Language);
        Assert.Equal(("Fake", "m1", "full.mp3"), (result.Provider, result.Model, result.PreparedAudioPath));
        Assert.Equal(new[] { "Preparing audio…", "Transcribing chunk 1 of 2…", "Transcribing chunk 2 of 2…" }, messages);
    }

    [Fact]
    public async Task Duration_falls_back_to_the_sum_of_chunk_durations()
    {
        var preparer = new FakeAudioPreparer(new AudioChunk("a.mp3", 0, 30), new AudioChunk("b.mp3", 30, null));
        var provider = new FakeTranscriptionProvider
        {
            OnTranscribe = (path, _) => new ProviderTranscription("x", [new TranscriptSegment(0, 1, "x")], null, path == "a.mp3" ? null : 12.5, "m"),
        };

        var result = await new TranscriptPipeline(preparer, provider, NullLogger<TranscriptPipeline>.Instance)
            .TranscribeAsync("in.mp3", "t", new TranscriptionRequest(), new AudioPreparationRequest("w", "a"), cancellationToken: Ct);

        Assert.Equal(42.5, result.DurationSeconds);
        Assert.Equal("in.mp3", result.PreparedAudioPath); // nothing was converted
    }

    [Fact]
    public async Task Rejects_translation_before_preparing_audio_when_unsupported()
    {
        var preparer = new FakeAudioPreparer();
        var provider = new FakeTranscriptionProvider { SupportsTranslation = false };
        var pipeline = new TranscriptPipeline(preparer, provider, NullLogger<TranscriptPipeline>.Instance);

        var error = await Assert.ThrowsAsync<TranscriptionException>(() => pipeline.TranscribeAsync(
            "in.mp3", "t", new TranscriptionRequest { Task = TranscriptionTask.Translate }, new AudioPreparationRequest("w", "a"), cancellationToken: Ct));

        Assert.Equal(TranscriptionErrorKind.NotSupported, error.Kind);
        Assert.Equal(0, preparer.Calls);
    }
}
