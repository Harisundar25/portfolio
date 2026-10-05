using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranscriptAI.Core.Models;

namespace TranscriptAI.Core.Formatting;

public enum TranscriptFormat
{
    /// <summary>Timestamped text, identical to the Python script's output.</summary>
    Txt,
    Srt,
    Vtt,
    Json,
}

/// <summary>Renders transcripts. <see cref="ToTimestampedText"/> reproduces the original Python output.</summary>
public static class TranscriptFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Port of the Python writer loop:
    /// <code>f.write(f"[{start:.2f} --&gt; {end:.2f}] {text}\n")</code>
    /// </summary>
    public static string ToTimestampedText(IEnumerable<TranscriptSegment> segments, string newLine = "\n")
    {
        ArgumentNullException.ThrowIfNull(segments);
        var builder = new StringBuilder();
        foreach (var segment in segments)
        {
            builder.Append(CultureInfo.InvariantCulture, $"[{segment.Start:F2} --> {segment.End:F2}] {segment.Text.Trim()}")
                   .Append(newLine);
        }

        return builder.ToString();
    }

    public static string ToSrt(IEnumerable<TranscriptSegment> segments, string newLine = "\n")
    {
        ArgumentNullException.ThrowIfNull(segments);
        var builder = new StringBuilder();
        var index = 1;
        foreach (var segment in segments)
        {
            builder.Append(index++).Append(newLine)
                   .Append(FormatTimestamp(segment.Start, ',')).Append(" --> ").Append(FormatTimestamp(segment.End, ','))
                   .Append(newLine)
                   .Append(segment.Text.Trim()).Append(newLine)
                   .Append(newLine);
        }

        return builder.ToString();
    }

    public static string ToWebVtt(IEnumerable<TranscriptSegment> segments, string newLine = "\n")
    {
        ArgumentNullException.ThrowIfNull(segments);
        var builder = new StringBuilder("WEBVTT").Append(newLine).Append(newLine);
        foreach (var segment in segments)
        {
            builder.Append(FormatTimestamp(segment.Start, '.')).Append(" --> ").Append(FormatTimestamp(segment.End, '.'))
                   .Append(newLine)
                   .Append(segment.Text.Trim()).Append(newLine)
                   .Append(newLine);
        }

        return builder.ToString();
    }

    /// <summary>Full result including language/duration metadata (the "metadata saved with transcripts" enhancement).</summary>
    public static string ToJson(TranscriptResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    public static string Format(TranscriptResult result, TranscriptFormat format, string newLine = "\n") => format switch
    {
        TranscriptFormat.Txt => ToTimestampedText(result.Segments, newLine),
        TranscriptFormat.Srt => ToSrt(result.Segments, newLine),
        TranscriptFormat.Vtt => ToWebVtt(result.Segments, newLine),
        TranscriptFormat.Json => ToJson(result),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };

    public static string FileExtension(TranscriptFormat format) => format switch
    {
        TranscriptFormat.Txt => ".txt",
        TranscriptFormat.Srt => ".srt",
        TranscriptFormat.Vtt => ".vtt",
        TranscriptFormat.Json => ".json",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
    };

    /// <summary>HH:MM:SS,mmm (SRT) or HH:MM:SS.mmm (WebVTT).</summary>
    internal static string FormatTimestamp(double seconds, char millisecondSeparator)
    {
        var totalMilliseconds = (long)Math.Round(Math.Max(0, seconds) * 1000, MidpointRounding.AwayFromZero);
        var hours = totalMilliseconds / 3_600_000;
        var minutes = totalMilliseconds / 60_000 % 60;
        var secs = totalMilliseconds / 1000 % 60;
        var millis = totalMilliseconds % 1000;
        return string.Create(CultureInfo.InvariantCulture, $"{hours:00}:{minutes:00}:{secs:00}{millisecondSeparator}{millis:000}");
    }
}
