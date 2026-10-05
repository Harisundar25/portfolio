namespace TranscriptAI.Core;

/// <summary>Broad failure categories, used by the API to pick an HTTP status code.</summary>
public enum TranscriptionErrorKind
{
    /// <summary>The service is misconfigured (missing/invalid API key, exhausted credits).</summary>
    Configuration,

    /// <summary>The caller supplied invalid input (bad URL, unknown language code…).</summary>
    InvalidInput,

    /// <summary>The media format cannot be processed in the current setup.</summary>
    UnsupportedMedia,

    /// <summary>The media is larger than the provider accepts and cannot be split.</summary>
    TooLarge,

    /// <summary>The media could not be fetched or decoded (private video, no audio track…).</summary>
    MediaUnavailable,

    /// <summary>The AI provider is rate limiting requests.</summary>
    RateLimited,

    /// <summary>The AI provider or YouTube failed unexpectedly.</summary>
    Upstream,

    /// <summary>The selected provider does not support the requested feature.</summary>
    NotSupported,
}

/// <summary>An expected, user-explainable failure in the transcription pipeline.</summary>
public sealed class TranscriptionException(string message, TranscriptionErrorKind kind, Exception? innerException = null)
    : Exception(message, innerException)
{
    public TranscriptionErrorKind Kind { get; } = kind;

    /// <summary>When rate limited, how long the provider asked us to wait.</summary>
    public TimeSpan? RetryAfter { get; init; }
}
