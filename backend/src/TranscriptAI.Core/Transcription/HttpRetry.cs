using System.Net;
using Microsoft.Extensions.Logging;
using TranscriptAI.Core.Options;

namespace TranscriptAI.Core.Transcription;

/// <summary>
/// Sends a request with retries on HTTP 429/5xx and network errors. The request is rebuilt
/// for every attempt (multipart file streams can only be sent once).
/// </summary>
internal static class HttpRetry
{
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        RetryOptions retry,
        ILogger logger,
        string providerName,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            using (var request = requestFactory())
            {
                try
                {
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                }
                catch (HttpRequestException ex) when (attempt < retry.MaxRetries)
                {
                    var delay = Backoff(retry, attempt);
                    logger.LogWarning(ex, "{Provider} request failed (attempt {Attempt}); retrying in {Delay}", providerName, attempt + 1, delay);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    throw new TranscriptionException($"Couldn't reach {providerName}: {ex.Message}", TranscriptionErrorKind.Upstream, ex);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TranscriptionException($"{providerName} did not respond in time.", TranscriptionErrorKind.Upstream, ex);
                }
            }

            if (!IsTransient(response.StatusCode) || attempt >= retry.MaxRetries)
            {
                return response;
            }

            var wait = GetRetryAfter(response) ?? Backoff(retry, attempt);
            if (wait > TimeSpan.FromSeconds(retry.MaxDelaySeconds))
            {
                // Waiting this long would exceed request timeouts upstream; let the caller report it.
                return response;
            }

            logger.LogWarning(
                "{Provider} returned {StatusCode} (attempt {Attempt}); retrying in {Delay}",
                providerName,
                (int)response.StatusCode,
                attempt + 1,
                wait);
            response.Dispose();
            await Task.Delay(wait, cancellationToken);
        }
    }

    internal static bool IsTransient(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.TooManyRequests or
        HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout;

    internal static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }

    internal static TimeSpan Backoff(RetryOptions retry, int attempt) =>
        TimeSpan.FromSeconds(retry.BaseDelaySeconds * Math.Pow(2, attempt));
}
