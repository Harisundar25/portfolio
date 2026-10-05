using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Options;
using TranscriptAI.Core.Transcription;
using TranscriptAI.Core.YouTube;

namespace TranscriptAI.Tests;

internal static class TestOptions
{
    public static IOptions<TranscriptionOptions> Create(Action<TranscriptionOptions>? configure = null)
    {
        var options = new TranscriptionOptions
        {
            Groq = { ApiKey = "test-groq-key" },
            HuggingFace = { ApiKey = "test-hf-token" },
            Retry = { MaxRetries = 2, BaseDelaySeconds = 0, MaxDelaySeconds = 5 },
        };
        configure?.Invoke(options);
        return Microsoft.Extensions.Options.Options.Create(options);
    }
}

/// <summary>A temporary folder that is deleted after the test.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "transcriptai-tests", Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public string CreateFile(string name, int sizeBytes = 16)
    {
        var path = System.IO.Path.Combine(Path, name);
        File.WriteAllBytes(path, new byte[sizeBytes]);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

/// <summary>An HTTP request as the provider sent it, with its body captured before disposal.</summary>
internal sealed record RecordedRequest(
    HttpMethod Method,
    Uri? Uri,
    string? Authorization,
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyDictionary<string, RecordedFile> Files,
    string? Body)
{
    public static async Task<RecordedRequest> CaptureAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>();
        var files = new Dictionary<string, RecordedFile>();
        string? body = null;

        if (request.Content is MultipartFormDataContent multipart)
        {
            foreach (var part in multipart)
            {
                var disposition = part.Headers.ContentDisposition!;
                var name = disposition.Name!.Trim('"');
                if (disposition.FileName is { } fileName)
                {
                    files[name] = new RecordedFile(
                        fileName.Trim('"'),
                        part.Headers.ContentType?.MediaType,
                        await part.ReadAsByteArrayAsync(cancellationToken));
                }
                else
                {
                    fields[name] = await part.ReadAsStringAsync(cancellationToken);
                }
            }
        }
        else if (request.Content is not null)
        {
            body = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new RecordedRequest(request.Method, request.RequestUri, request.Headers.Authorization?.ToString(), fields, files, body);
    }
}

internal sealed record RecordedFile(string FileName, string? ContentType, byte[] Content);

/// <summary>Records requests and replies with queued responses; the last one repeats.</summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public FakeHttpHandler Respond(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responses.Enqueue(responder);
        return this;
    }

    public FakeHttpHandler RespondJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        Respond(_ => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public FakeHttpHandler RespondStatus(HttpStatusCode status, TimeSpan? retryAfter = null, string body = "") =>
        Respond(_ =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
            if (retryAfter is { } delay)
            {
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(delay);
            }

            return response;
        });

    public FakeHttpHandler Throw(Exception exception) => Respond(_ => throw exception);

    public HttpClient CreateClient(string baseAddress) => new(this) { BaseAddress = new Uri(baseAddress) };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(await RecordedRequest.CaptureAsync(request, cancellationToken));
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No response queued.");
        }

        var responder = _responses.Count == 1 ? _responses.Peek() : _responses.Dequeue();
        return responder(request);
    }
}

internal sealed class StubFFmpegLocator(string? path) : IFFmpegLocator
{
    public string? FindFFmpeg() => path;
}

internal sealed class FakeProcessRunner(Func<IReadOnlyList<string>, ProcessResult> handler) : IProcessRunner
{
    public List<IReadOnlyList<string>> Calls { get; } = [];

    public Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        Calls.Add(arguments);
        return Task.FromResult(handler(arguments));
    }
}

internal sealed class FakeAudioPreparer(params AudioChunk[] chunks) : IAudioPreparer
{
    public bool CanConvert { get; init; } = true;

    public double? TotalDurationSeconds { get; init; }

    public string? ConvertedPath { get; init; }

    public int Calls { get; private set; }

    public Task<PreparedAudio> PrepareAsync(string inputPath, AudioPreparationRequest request, CancellationToken cancellationToken = default)
    {
        Calls++;
        IReadOnlyList<AudioChunk> prepared = chunks.Length > 0 ? chunks : [new AudioChunk(inputPath, 0, null)];
        return Task.FromResult(new PreparedAudio(prepared, TotalDurationSeconds, ConvertedPath));
    }
}

internal sealed class FakeTranscriptionProvider : ITranscriptionProvider
{
    private readonly Lock _gate = new();

    public string Name { get; init; } = "Fake";

    public bool SupportsTranslation { get; init; } = true;

    public bool IsConfigured { get; set; } = true;

    /// <summary>Builds the result for a call; defaults to one "Hello world" segment.</summary>
    public Func<string, TranscriptionRequest, ProviderTranscription>? OnTranscribe { get; set; }

    /// <summary>Awaited before replying, so tests can hold requests in flight.</summary>
    public Func<Task>? Gate { get; set; }

    public Exception? Exception { get; set; }

    public List<(string AudioPath, bool AudioExisted, TranscriptionRequest Request)> Calls { get; } = [];

    public string DefaultModel(TranscriptionTask task) => task == TranscriptionTask.Translate ? "fake-translate" : "fake-model";

    public async Task<ProviderTranscription> TranscribeAsync(
        string audioPath,
        TranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            Calls.Add((audioPath, File.Exists(audioPath), request));
        }

        if (Gate is not null)
        {
            await Gate();
        }

        if (Exception is not null)
        {
            throw Exception;
        }

        return OnTranscribe?.Invoke(audioPath, request)
            ?? new ProviderTranscription("Hello world", [new TranscriptSegment(0, 1.5, "Hello world")], "english", 1.5, DefaultModel(request.Task));
    }
}

internal sealed class FakeYouTubeDownloader : IYouTubeAudioDownloader
{
    public YouTubeVideoInfo Info { get; set; } = new("dQw4w9WgXcQ", "Test: a video?", "Test Channel", TimeSpan.FromMinutes(3));

    public Exception? Exception { get; set; }

    public int Downloads { get; private set; }

    public Task<YouTubeVideoInfo> GetInfoAsync(string url, CancellationToken cancellationToken = default) =>
        Exception is not null ? Task.FromException<YouTubeVideoInfo>(Exception) : Task.FromResult(Info);

    public async Task<DownloadedAudio> DownloadAudioAsync(
        string url,
        string outputDirectory,
        string fileNameWithoutExtension,
        long? preferredMaxBytes,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Downloads++;
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, fileNameWithoutExtension + ".m4a");
        await File.WriteAllBytesAsync(path, new byte[1024], cancellationToken);
        return new DownloadedAudio(path, 1024, 64_000);
    }
}
