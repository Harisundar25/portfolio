using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Options;

namespace TranscriptAI.Core.Transcription;

/// <summary>
/// Alternative provider: Whisper through Hugging Face Inference Providers
/// (<c>router.huggingface.co/hf-inference/models/openai/whisper-large-v3</c>).
/// Timestamps come from <c>return_timestamps</c> "chunks". Translation and language hints are not exposed by this API.
/// </summary>
public sealed class HuggingFaceTranscriptionProvider(
    HttpClient httpClient,
    IOptions<TranscriptionOptions> options,
    ILogger<HuggingFaceTranscriptionProvider> logger) : ITranscriptionProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "Hugging Face";

    public bool SupportsTranslation => false;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.HuggingFace.ApiKey);

    public string DefaultModel(TranscriptionTask task) => options.Value.HuggingFace.Model;

    public async Task<ProviderTranscription> TranscribeAsync(
        string audioPath,
        TranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioPath);
        ArgumentNullException.ThrowIfNull(request);

        var settings = options.Value;
        if (request.Task == TranscriptionTask.Translate)
        {
            throw new TranscriptionException(
                "Translation isn't available with the Hugging Face provider. Set Transcription:Provider to Groq to translate.",
                TranscriptionErrorKind.NotSupported);
        }

        var token = settings.HuggingFace.ApiKey;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new TranscriptionException(
                "Hugging Face token is not configured. Set HF_TOKEN (CLI) or Transcription__HuggingFace__ApiKey (API).",
                TranscriptionErrorKind.Configuration);
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            logger.LogWarning("Language hints are ignored by the Hugging Face provider; Whisper will auto-detect the language.");
        }

        var model = string.IsNullOrWhiteSpace(request.Model) ? settings.HuggingFace.Model : request.Model;
        var audio = await File.ReadAllBytesAsync(audioPath, cancellationToken);
        var payload = JsonSerializer.Serialize(new
        {
            inputs = Convert.ToBase64String(audio),
            parameters = new { return_timestamps = true },
        });

        using var response = await HttpRetry.SendAsync(
            httpClient,
            () =>
            {
                var message = new HttpRequestMessage(HttpMethod.Post, model)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return message;
            },
            settings.Retry,
            logger,
            Name,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await ProviderErrors.FromResponseAsync(response, Name, cancellationToken);
        }

        var body = await response.Content.ReadFromJsonAsync<AsrResponse>(JsonOptions, cancellationToken)
            ?? throw new TranscriptionException("Hugging Face returned an empty response.", TranscriptionErrorKind.Upstream);

        var segments = new List<TranscriptSegment>();
        foreach (var chunk in body.Chunks ?? [])
        {
            var text = chunk.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            var start = chunk.Timestamp is { Length: > 0 } ts && ts[0] is { } s ? s : segments.LastOrDefault()?.End ?? 0;
            var end = chunk.Timestamp is { Length: > 1 } te && te[1] is { } e ? e : start;
            segments.Add(new TranscriptSegment(start, end, text));
        }

        var fullText = body.Text?.Trim() ?? string.Empty;
        if (segments.Count == 0 && fullText.Length > 0)
        {
            segments.Add(new TranscriptSegment(0, 0, fullText));
        }

        return new ProviderTranscription(
            fullText.Length > 0 ? fullText : string.Join(" ", segments.Select(s => s.Text)),
            segments,
            Language: null,
            DurationSeconds: segments.Count > 0 ? segments[^1].End : null,
            model);
    }

    private sealed record AsrResponse(string? Text, List<AsrChunk>? Chunks);

    private sealed record AsrChunk(string? Text, double?[]? Timestamp);
}
