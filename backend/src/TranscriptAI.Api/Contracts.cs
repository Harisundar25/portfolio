using System.Text.Json.Serialization;
using TranscriptAI.Core.Formatting;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Text;

namespace TranscriptAI.Api;

public sealed record YouTubeTranscriptionRequest(string? Url, string? Task = null, string? Language = null);

public sealed record TranscriptSegmentDto(double Start, double End, string Text);

/// <summary>Ready-to-download renderings of the transcript.</summary>
public sealed record TranscriptDocuments(string Txt, string Srt, string Vtt);

public sealed record TranscriptionResponse(
    string Title,
    string FileName,
    string Source,
    string Task,
    string? Language,
    double? DurationSeconds,
    string Provider,
    string Model,
    int ChunkCount,
    IReadOnlyList<TranscriptSegmentDto> Segments,
    string Text,
    TranscriptDocuments Documents,
    long ElapsedMilliseconds)
{
    public static TranscriptionResponse From(TranscriptResult result, string source, long elapsedMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TranscriptionResponse(
            result.Title,
            FileNameSanitizer.Sanitize(result.Title),
            source,
            result.Task == TranscriptionTask.Translate ? "translate" : "transcribe",
            result.Language,
            result.DurationSeconds is { } duration ? Math.Round(duration, 2) : null,
            result.Provider,
            result.Model,
            result.ChunkCount,
            result.Segments.Select(s => new TranscriptSegmentDto(s.Start, s.End, s.Text)).ToList(),
            result.Text,
            new TranscriptDocuments(
                TranscriptFormatter.ToTimestampedText(result.Segments),
                TranscriptFormatter.ToSrt(result.Segments),
                TranscriptFormatter.ToWebVtt(result.Segments)),
            elapsedMilliseconds);
    }
}

public sealed record HealthResponse(
    string Status,
    string Version,
    string Provider,
    bool ProviderConfigured,
    string Model,
    string TranslationModel,
    bool SupportsTranslation,
    [property: JsonPropertyName("ffmpegAvailable")] bool FFmpegAvailable,
    bool YouTubeEnabled,
    int MaxUploadMegabytes,
    int MaxDurationMinutes,
    IReadOnlyCollection<string> AcceptedExtensions);
