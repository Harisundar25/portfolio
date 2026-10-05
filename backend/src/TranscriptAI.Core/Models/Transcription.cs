using System.Text.Json.Serialization;

namespace TranscriptAI.Core.Models;

/// <summary>What the speech model should do with the audio.</summary>
public enum TranscriptionTask
{
    /// <summary>Transcribe in the spoken language (Whisper <c>task="transcribe"</c>).</summary>
    Transcribe,

    /// <summary>Translate any spoken language into English text (Whisper <c>task="translate"</c>).</summary>
    Translate,
}

/// <summary>One timestamped piece of the transcript — mirrors a Whisper "segment".</summary>
/// <param name="Start">Start time in seconds.</param>
/// <param name="End">End time in seconds.</param>
/// <param name="Text">Recognised text (trimmed).</param>
public sealed record TranscriptSegment(double Start, double End, string Text);

/// <summary>Options for a single transcription.</summary>
public sealed record TranscriptionRequest
{
    public TranscriptionTask Task { get; init; } = TranscriptionTask.Transcribe;

    /// <summary>ISO-639-1 source-language hint (e.g. "en", "ta"). <c>null</c> lets Whisper auto-detect.</summary>
    public string? Language { get; init; }

    /// <summary>Optional context or spelling hints passed to the model.</summary>
    public string? Prompt { get; init; }

    /// <summary>Overrides the provider's default model.</summary>
    public string? Model { get; init; }
}

/// <summary>Result of one provider call (one audio file or chunk).</summary>
public sealed record ProviderTranscription(
    string Text,
    IReadOnlyList<TranscriptSegment> Segments,
    string? Language,
    double? DurationSeconds,
    string Model);

/// <summary>The final transcript for a media item.</summary>
public sealed record TranscriptResult
{
    public required string Title { get; init; }

    public required IReadOnlyList<TranscriptSegment> Segments { get; init; }

    public required string Text { get; init; }

    /// <summary>Language reported by the model (auto-detected unless a hint was supplied).</summary>
    public string? Language { get; init; }

    public double? DurationSeconds { get; init; }

    public required string Provider { get; init; }

    public required string Model { get; init; }

    public TranscriptionTask Task { get; init; }

    /// <summary>Number of audio chunks sent to the API (more than one for long recordings).</summary>
    public int ChunkCount { get; init; } = 1;

    /// <summary>Local path of the audio actually sent (the FFmpeg-converted MP3 when available).</summary>
    [JsonIgnore]
    public string? PreparedAudioPath { get; init; }
}
