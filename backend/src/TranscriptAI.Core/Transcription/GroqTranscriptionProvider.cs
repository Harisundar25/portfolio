using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Options;

namespace TranscriptAI.Core.Transcription;

/// <summary>
/// Open-source Whisper models served by Groq's OpenAI-compatible API (free tier available).
/// Uses <c>response_format=verbose_json</c> to get the same start/end segments the Python
/// script read from <c>result["segments"]</c>.
/// </summary>
public sealed class GroqTranscriptionProvider(
    HttpClient httpClient,
    IOptions<TranscriptionOptions> options,
    ILogger<GroqTranscriptionProvider> logger) : ITranscriptionProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "Groq";

    public bool SupportsTranslation => true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.Groq.ApiKey);

    public string DefaultModel(TranscriptionTask task) =>
        task == TranscriptionTask.Translate ? options.Value.Groq.TranslationModel : options.Value.Groq.Model;

    public async Task<ProviderTranscription> TranscribeAsync(
        string audioPath,
        TranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioPath);
        ArgumentNullException.ThrowIfNull(request);

        var settings = options.Value;
        var apiKey = settings.Groq.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new TranscriptionException(
                "Groq API key is not configured. Create a free key at https://console.groq.com/keys and set " +
                "GROQ_API_KEY (CLI) or Transcription__Groq__ApiKey (API).",
                TranscriptionErrorKind.Configuration);
        }

        var translate = request.Task == TranscriptionTask.Translate;
        var model = string.IsNullOrWhiteSpace(request.Model) ? DefaultModel(request.Task) : request.Model;
        var endpoint = translate ? "audio/translations" : "audio/transcriptions";

        using var response = await HttpRetry.SendAsync(
            httpClient,
            () => BuildRequest(endpoint, apiKey, audioPath, model, request, translate),
            settings.Retry,
            logger,
            Name,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ProviderErrors.FromResponseAsync(response, Name, cancellationToken);
        }

        var payload = await response.Content.ReadFromJsonAsync<VerboseJsonResponse>(JsonOptions, cancellationToken)
            ?? throw new TranscriptionException("Groq returned an empty response.", TranscriptionErrorKind.Upstream);

        var segments = (payload.Segments ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select(s => new TranscriptSegment(s.Start, s.End, s.Text!.Trim()))
            .ToList();

        // Some responses carry text without segments; keep the transcript rather than dropping it.
        if (segments.Count == 0 && !string.IsNullOrWhiteSpace(payload.Text))
        {
            segments.Add(new TranscriptSegment(0, payload.Duration ?? 0, payload.Text.Trim()));
        }

        return new ProviderTranscription(
            payload.Text?.Trim() ?? string.Join(" ", segments.Select(s => s.Text)),
            segments,
            translate ? "English" : payload.Language,
            payload.Duration,
            model);
    }

    private static HttpRequestMessage BuildRequest(
        string endpoint,
        string apiKey,
        string audioPath,
        string model,
        TranscriptionRequest request,
        bool translate)
    {
        var content = new MultipartFormDataContent();

        var file = new StreamContent(File.OpenRead(audioPath));
        file.Headers.ContentType = new MediaTypeHeaderValue(AudioFormats.GetContentType(audioPath));
        // A neutral ASCII file name avoids encoding issues with non-English titles; the extension tells Groq the format.
        content.Add(file, "file", "audio" + Path.GetExtension(audioPath).ToLowerInvariant());

        content.Add(new StringContent(model), "model");
        content.Add(new StringContent("verbose_json"), "response_format");
        content.Add(new StringContent("0"), "temperature");

        if (!translate)
        {
            content.Add(new StringContent("segment"), "timestamp_granularities[]");
            if (!string.IsNullOrWhiteSpace(request.Language))
            {
                content.Add(new StringContent(request.Language.ToLower(CultureInfo.InvariantCulture)), "language");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            content.Add(new StringContent(request.Prompt), "prompt");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return message;
    }

    private sealed record VerboseJsonResponse(string? Text, string? Language, double? Duration, List<Segment>? Segments);

    private sealed record Segment(double Start, double End, string? Text);
}
