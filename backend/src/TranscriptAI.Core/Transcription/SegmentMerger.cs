using TranscriptAI.Core.Models;

namespace TranscriptAI.Core.Transcription;

/// <summary>Stitches per-chunk segments back onto the original recording's timeline.</summary>
public static class SegmentMerger
{
    public static IReadOnlyList<TranscriptSegment> Merge(
        IEnumerable<(double OffsetSeconds, IReadOnlyList<TranscriptSegment> Segments)> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var merged = new List<TranscriptSegment>();

        foreach (var (offset, segments) in parts)
        {
            foreach (var segment in segments)
            {
                var text = segment.Text.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                merged.Add(new TranscriptSegment(
                    Math.Round(segment.Start + offset, 3),
                    Math.Round(segment.End + offset, 3),
                    text));
            }
        }

        return merged;
    }
}
