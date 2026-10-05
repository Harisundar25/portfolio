using Microsoft.Extensions.Logging;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Transcription;

namespace TranscriptAI.Core.Pipeline;

/// <summary>
/// The heart of the C# port: prepare audio → transcribe each chunk with the hosted
/// Whisper model → merge timestamped segments. Shared by the CLI and the web API.
/// </summary>
public sealed class TranscriptPipeline(
    IAudioPreparer audioPreparer,
    ITranscriptionProvider provider,
    ILogger<TranscriptPipeline> logger)
{
    public ITranscriptionProvider Provider => provider;

    public bool CanConvertMedia => audioPreparer.CanConvert;

    public async Task<TranscriptResult> TranscribeAsync(
        string audioPath,
        string title,
        TranscriptionRequest request,
        AudioPreparationRequest preparation,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(preparation);

        if (request.Task == TranscriptionTask.Translate && !provider.SupportsTranslation)
        {
            throw new TranscriptionException(
                $"{provider.Name} doesn't support translation. Use the Groq provider to translate audio to English.",
                TranscriptionErrorKind.NotSupported);
        }

        progress?.Report("Preparing audio…");
        using var prepared = await audioPreparer.PrepareAsync(audioPath, preparation, cancellationToken);

        var parts = new List<(AudioChunk Chunk, ProviderTranscription Result)>(prepared.Chunks.Count);
        for (var i = 0; i < prepared.Chunks.Count; i++)
        {
            var chunk = prepared.Chunks[i];
            progress?.Report(prepared.Chunks.Count == 1
                ? $"Transcribing with {provider.Name} ({provider.DefaultModel(request.Task)})…"
                : $"Transcribing chunk {i + 1} of {prepared.Chunks.Count}…");

            var result = await provider.TranscribeAsync(chunk.Path, request, cancellationToken);
            logger.LogDebug("Chunk {Index} returned {Count} segments", i + 1, result.Segments.Count);
            parts.Add((chunk, result));
        }

        var segments = SegmentMerger.Merge(parts.Select(p => (p.Chunk.OffsetSeconds, p.Result.Segments)));
        var text = string.Join(" ", parts.Select(p => p.Result.Text.Trim()).Where(t => t.Length > 0));

        return new TranscriptResult
        {
            Title = title,
            Segments = segments,
            Text = text,
            Language = parts.Select(p => p.Result.Language).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)),
            DurationSeconds = prepared.TotalDurationSeconds ?? SumDurations(parts) ?? segments.LastOrDefault()?.End,
            Provider = provider.Name,
            Model = parts.Count > 0 ? parts[0].Result.Model : provider.DefaultModel(request.Task),
            Task = request.Task,
            ChunkCount = parts.Count,
            PreparedAudioPath = prepared.ConvertedPath ?? audioPath,
        };
    }

    private static double? SumDurations(List<(AudioChunk Chunk, ProviderTranscription Result)> parts)
    {
        double total = 0;
        foreach (var (chunk, result) in parts)
        {
            var duration = result.DurationSeconds ?? chunk.DurationSeconds;
            if (duration is null)
            {
                return null;
            }

            total += duration.Value;
        }

        return parts.Count > 0 ? total : null;
    }
}
