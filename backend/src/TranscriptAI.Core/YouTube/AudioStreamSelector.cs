namespace TranscriptAI.Core.YouTube;

/// <summary>Library-independent view of an audio-only stream.</summary>
public sealed record AudioStreamCandidate(int Index, long SizeBytes, long BitsPerSecond, string Container);

/// <summary>
/// The Python script used 'bestaudio' and re-encoded to 192 kbps MP3. Speech recognition doesn't need that much:
/// we pick the smallest stream of at least 48 kbps, and when a size limit is given (no FFmpeg to split later)
/// the largest stream that still fits under it.
/// </summary>
public static class AudioStreamSelector
{
    public const long MinimumSpeechBitrate = 48_000;

    public static AudioStreamCandidate? Select(IReadOnlyCollection<AudioStreamCandidate> streams, long? maxBytes)
    {
        ArgumentNullException.ThrowIfNull(streams);
        if (streams.Count == 0)
        {
            return null;
        }

        var bySize = streams.OrderBy(s => s.SizeBytes).ToList();
        var preferred = bySize.FirstOrDefault(s => s.BitsPerSecond >= MinimumSpeechBitrate) ?? bySize[^1];

        if (maxBytes is { } limit && preferred.SizeBytes > limit)
        {
            return bySize.LastOrDefault(s => s.SizeBytes <= limit) ?? bySize[0];
        }

        return preferred;
    }
}
