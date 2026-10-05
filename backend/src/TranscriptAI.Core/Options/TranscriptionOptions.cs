namespace TranscriptAI.Core.Options;

public enum TranscriptionProviderKind
{
    Groq,
    HuggingFace,
}

/// <summary>
/// Settings bound from the <c>Transcription</c> configuration section, e.g.
/// <c>Transcription__Groq__ApiKey</c> as an environment variable.
/// </summary>
public sealed class TranscriptionOptions
{
    public const string SectionName = "Transcription";

    /// <summary>Which hosted model provider performs inference.</summary>
    public TranscriptionProviderKind Provider { get; set; } = TranscriptionProviderKind.Groq;

    /// <summary>Largest file sent to the provider in one request (Groq free tier: 25 MB).</summary>
    public int MaxUploadMegabytes { get; set; } = 25;

    /// <summary>MP3 bitrate used when FFmpeg downsamples audio (16 kHz mono speech needs very little).</summary>
    public int AudioBitrateKbps { get; set; } = 32;

    /// <summary>HTTP timeout for a single provider request.</summary>
    public int RequestTimeoutSeconds { get; set; } = 300;

    public GroqOptions Groq { get; set; } = new();

    public HuggingFaceOptions HuggingFace { get; set; } = new();

    public FFmpegOptions FFmpeg { get; set; } = new();

    public RetryOptions Retry { get; set; } = new();

    /// <summary>Upload limit in bytes (decimal megabytes, slightly conservative).</summary>
    public long MaxUploadBytes => MaxUploadMegabytes * 1_000_000L;
}

public sealed class GroqOptions
{
    /// <summary>Free key from https://console.groq.com/keys. Falls back to the GROQ_API_KEY environment variable.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";

    /// <summary>Fast multilingual transcription model.</summary>
    public string Model { get; set; } = "whisper-large-v3-turbo";

    /// <summary>Translation to English needs whisper-large-v3 (the turbo model cannot translate).</summary>
    public string TranslationModel { get; set; } = "whisper-large-v3";
}

public sealed class HuggingFaceOptions
{
    /// <summary>Access token with "Inference Providers" permission. Falls back to the HF_TOKEN environment variable.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://router.huggingface.co/hf-inference/models/";

    public string Model { get; set; } = "openai/whisper-large-v3";
}

public sealed class FFmpegOptions
{
    /// <summary>Path to ffmpeg(.exe) or the folder containing it. Empty = search PATH.</summary>
    public string? Path { get; set; }
}

public sealed class RetryOptions
{
    /// <summary>Retries for HTTP 429 / 5xx / network failures.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>First back-off delay; doubles on each attempt unless the server sends Retry-After.</summary>
    public double BaseDelaySeconds { get; set; } = 2;

    /// <summary>Never wait longer than this between attempts; longer Retry-After values fail fast instead.</summary>
    public double MaxDelaySeconds { get; set; } = 20;
}
