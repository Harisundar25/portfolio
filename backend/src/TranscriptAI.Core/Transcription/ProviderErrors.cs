using System.Net;
using System.Text.Json;

namespace TranscriptAI.Core.Transcription;

/// <summary>Maps provider error responses to <see cref="TranscriptionException"/>.</summary>
internal static class ProviderErrors
{
    public static async Task<TranscriptionException> FromResponseAsync(
        HttpResponseMessage response,
        string providerName,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = ExtractMessage(body) ?? response.ReasonPhrase ?? "Unknown error";
        var status = (int)response.StatusCode;

        var kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.PaymentRequired => TranscriptionErrorKind.Configuration,
            HttpStatusCode.RequestEntityTooLarge => TranscriptionErrorKind.TooLarge,
            HttpStatusCode.TooManyRequests => TranscriptionErrorKind.RateLimited,
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.UnsupportedMediaType => TranscriptionErrorKind.InvalidInput,
            _ => TranscriptionErrorKind.Upstream,
        };

        var message = kind switch
        {
            TranscriptionErrorKind.Configuration => $"{providerName} rejected the request ({status}): {AsSentence(detail)} Check the API key / account configuration.",
            TranscriptionErrorKind.RateLimited => $"{providerName} rate limit reached: {detail}",
            TranscriptionErrorKind.TooLarge => $"{providerName} says the audio is too large: {detail}",
            _ => $"{providerName} returned HTTP {status}: {detail}",
        };

        return new TranscriptionException(message, kind) { RetryAfter = HttpRetry.GetRetryAfter(response) };
    }

    private static string AsSentence(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?') ? trimmed : trimmed + ".";
    }

    /// <summary>
    /// Handles OpenAI/Groq style <c>{"error":{"message":"..."}}</c> and Hugging Face style
    /// <c>{"error":"..."}</c> bodies, falling back to the raw (truncated) text.
    /// </summary>
    internal static string? ExtractMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("error", out var error))
            {
                return error.ValueKind switch
                {
                    JsonValueKind.String => error.GetString(),
                    JsonValueKind.Object when error.TryGetProperty("message", out var message) => message.GetString(),
                    JsonValueKind.Array => string.Join("; ", error.EnumerateArray().Select(e => e.ToString())),
                    _ => error.ToString(),
                };
            }
        }
        catch (JsonException)
        {
            // Not JSON — use the raw text below.
        }

        var text = body.Trim();
        return text.Length <= 300 ? text : text[..300] + "…";
    }
}
