using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using TranscriptAI.Core;

namespace TranscriptAI.Api;

/// <summary>Turns expected pipeline failures into RFC 7807 problem responses.</summary>
internal sealed class TranscriptionExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<TranscriptionExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The visitor closed the page or cancelled; nobody is waiting for a response.
            httpContext.Response.StatusCode = 499;
            return true;
        }

        if (exception is not TranscriptionException error)
        {
            return false;
        }

        var (status, title) = Map(error.Kind);
        if (error.Kind is TranscriptionErrorKind.Configuration or TranscriptionErrorKind.Upstream)
        {
            logger.LogError(error, "Transcription failed ({Kind})", error.Kind);
        }
        else
        {
            logger.LogInformation("Transcription rejected ({Kind}): {Message}", error.Kind, error.Message);
        }

        if (error.RetryAfter is { } retryAfter)
        {
            httpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        // Configuration problems are logged in full but not exposed to visitors.
        var detail = error.Kind == TranscriptionErrorKind.Configuration
            ? "The transcription service isn't configured correctly right now. Please try again later."
            : error.Message;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = error,
            ProblemDetails = { Status = status, Title = title, Detail = detail },
        });
    }

    internal static (int Status, string Title) Map(TranscriptionErrorKind kind) => kind switch
    {
        TranscriptionErrorKind.InvalidInput => (StatusCodes.Status400BadRequest, "Invalid request"),
        TranscriptionErrorKind.NotSupported => (StatusCodes.Status400BadRequest, "Not supported"),
        TranscriptionErrorKind.UnsupportedMedia => (StatusCodes.Status415UnsupportedMediaType, "Unsupported media"),
        TranscriptionErrorKind.TooLarge => (StatusCodes.Status413PayloadTooLarge, "Media too large"),
        TranscriptionErrorKind.MediaUnavailable => (StatusCodes.Status422UnprocessableEntity, "Media unavailable"),
        TranscriptionErrorKind.RateLimited => (StatusCodes.Status429TooManyRequests, "AI provider rate limit"),
        TranscriptionErrorKind.Upstream => (StatusCodes.Status502BadGateway, "Upstream service error"),
        TranscriptionErrorKind.Configuration => (StatusCodes.Status503ServiceUnavailable, "Service unavailable"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
    };
}
