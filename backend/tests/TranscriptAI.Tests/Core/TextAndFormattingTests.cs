using System.Globalization;
using TranscriptAI.Core.Formatting;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Text;
using TranscriptAI.Core.Transcription;

namespace TranscriptAI.Tests.Core;

public sealed class FileNameSanitizerTests
{
    [Fact]
    public void Removes_the_same_characters_as_the_python_regex()
    {
        Assert.Equal("abcdefghij", FileNameSanitizer.Sanitize("a\\b/c*d?e:f\"g<h>i|j"));
    }

    [Fact]
    public void Normalizes_full_width_characters_like_unicodedata_nfkd()
    {
        // Full-width letters and colon (common in video titles) become ASCII, then ':' is removed.
        Assert.Equal("HelloWorld", FileNameSanitizer.Sanitize("Ｈｅｌｌｏ：Ｗｏｒｌｄ"));
    }

    [Fact]
    public void Strips_control_characters_and_trailing_dots_and_spaces()
    {
        Assert.Equal("Report final", FileNameSanitizer.Sanitize("Report\u0007 final. . "));
    }

    [Theory]
    [InlineData("CON", "_CON")]
    [InlineData("nul.txt", "_nul.txt")]
    [InlineData("Console", "Console")]
    public void Prefixes_reserved_windows_device_names(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.Sanitize(input));
    }

    [Fact]
    public void Falls_back_when_nothing_is_left()
    {
        Assert.Equal("transcript", FileNameSanitizer.Sanitize("???"));
        Assert.Equal("untitled", FileNameSanitizer.Sanitize(" :: ", fallback: "untitled"));
    }

    [Fact]
    public void Caps_very_long_titles()
    {
        Assert.Equal(150, FileNameSanitizer.Sanitize(new string('a', 400)).Length);
    }
}

public sealed class TranscriptFormatterTests
{
    private static readonly TranscriptSegment[] Segments =
    [
        new(0, 2.5, " Hello "),
        new(2.5, 61.237, "World"),
    ];

    [Fact]
    public void Timestamped_text_matches_the_python_output_format()
    {
        // run.py: f.write(f"[{start:.2f} --> {end:.2f}] {text}\n")
        Assert.Equal(
            "[0.00 --> 2.50] Hello\n[2.50 --> 61.24] World\n",
            TranscriptFormatter.ToTimestampedText(Segments));
    }

    [Fact]
    public void Output_is_culture_invariant()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.StartsWith("[0.00 --> 2.50]", TranscriptFormatter.ToTimestampedText(Segments));
            Assert.Contains("00:00:02,500", TranscriptFormatter.ToSrt(Segments));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Srt_numbers_cues_and_uses_comma_milliseconds()
    {
        Assert.Equal(
            "1\n00:00:00,000 --> 00:00:02,500\nHello\n\n2\n00:00:02,500 --> 00:01:01,237\nWorld\n\n",
            TranscriptFormatter.ToSrt(Segments));
    }

    [Fact]
    public void WebVtt_has_a_header_and_dot_milliseconds()
    {
        Assert.Equal(
            "WEBVTT\n\n00:00:00.000 --> 00:00:02.500\nHello\n\n00:00:02.500 --> 00:01:01.237\nWorld\n\n",
            TranscriptFormatter.ToWebVtt(Segments));
    }

    [Theory]
    [InlineData(3725.5, ',', "01:02:05,500")]
    [InlineData(59.9996, '.', "00:01:00.000")]
    [InlineData(-3, '.', "00:00:00.000")]
    public void Formats_subtitle_timestamps(double seconds, char separator, string expected)
    {
        Assert.Equal(expected, TranscriptFormatter.FormatTimestamp(seconds, separator));
    }

    [Fact]
    public void Json_includes_metadata_but_not_local_paths()
    {
        var result = new TranscriptResult
        {
            Title = "Talk",
            Segments = Segments,
            Text = "Hello World",
            Language = "english",
            Provider = "Groq",
            Model = "whisper-large-v3",
            Task = TranscriptionTask.Translate,
            PreparedAudioPath = @"C:\secret\audio.mp3",
        };

        var json = TranscriptFormatter.ToJson(result);

        Assert.Contains("\"task\": \"translate\"", json);
        Assert.Contains("\"segments\": [", json);
        Assert.DoesNotContain("secret", json);
    }

    [Theory]
    [InlineData(TranscriptFormat.Txt, ".txt")]
    [InlineData(TranscriptFormat.Srt, ".srt")]
    [InlineData(TranscriptFormat.Vtt, ".vtt")]
    [InlineData(TranscriptFormat.Json, ".json")]
    public void Maps_formats_to_file_extensions(TranscriptFormat format, string extension)
    {
        Assert.Equal(extension, TranscriptFormatter.FileExtension(format));
    }
}

public sealed class SegmentMergerTests
{
    [Fact]
    public void Offsets_chunk_segments_and_drops_empty_text()
    {
        var merged = SegmentMerger.Merge(
        [
            (0d, [new TranscriptSegment(0, 1, "First"), new TranscriptSegment(1, 2, "   ")]),
            (600d, [new TranscriptSegment(0.1234, 1.9876, " Second ")]),
        ]);

        Assert.Equal(
            new[] { new TranscriptSegment(0, 1, "First"), new TranscriptSegment(600.123, 601.988, "Second") },
            merged);
    }
}
