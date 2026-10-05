using TranscriptAI.Cli;
using TranscriptAI.Core.Formatting;

namespace TranscriptAI.Tests.Cli;

public sealed class CliParserTests
{
    [Fact]
    public void Defaults_match_the_python_script_folders()
    {
        var options = CliParser.Parse([]).Options!;

        Assert.Null(options.Input); // prompts like run.py's input()
        Assert.Equal(new[] { TranscriptFormat.Txt }, options.Formats);
        Assert.Equal("Transcripts", options.TranscriptDirectory);
        Assert.Equal("AudioFiles", options.AudioDirectory);
    }

    [Fact]
    public void Parses_input_and_options()
    {
        var result = CliParser.Parse(
        [
            "lecture.mp4", "-l", "TA", "--format=txt,SRT", "-t", "--provider", "hf",
            "-o", "out", "-a", "audio", "--ffmpeg", "C:/ffmpeg/bin", "-m", "whisper-large-v3", "--prompt", "GRL, Qi", "-v",
        ]);

        Assert.Null(result.Error);
        var options = result.Options!;
        Assert.Equal("lecture.mp4", options.Input);
        Assert.Equal("ta", options.Language);
        Assert.Equal(new[] { TranscriptFormat.Txt, TranscriptFormat.Srt }, options.Formats);
        Assert.True(options.Translate);
        Assert.Equal("HuggingFace", options.Provider);
        Assert.Equal(("out", "audio"), (options.TranscriptDirectory, options.AudioDirectory));
        Assert.Equal("C:/ffmpeg/bin", options.FFmpegPath);
        Assert.Equal("whisper-large-v3", options.Model);
        Assert.Equal("GRL, Qi", options.Prompt);
        Assert.True(options.Verbose);
    }

    [Fact]
    public void All_selects_every_format_and_duplicates_are_ignored()
    {
        Assert.Equal(4, CliParser.Parse(["-f", "all"]).Options!.Formats.Count);
        Assert.Equal(new[] { TranscriptFormat.Vtt }, CliParser.ParseFormats("vtt, VTT"));
    }

    public static TheoryData<string[]> InvalidArguments => new()
    {
        new[] { "--language", "english" },
        new[] { "--format", "pdf" },
        new[] { "--format", "1" },
        new[] { "--provider", "openai" },
        new[] { "--model" },
        new[] { "--bogus" },
        new[] { "a.mp3", "b.mp3" },
    };

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void Rejects_invalid_arguments(string[] args)
    {
        var result = CliParser.Parse(args);

        Assert.Null(result.Options);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Help_flags_show_help(string flag)
    {
        Assert.True(CliParser.Parse(["video.mp4", flag]).ShowHelp);
    }

    [Fact]
    public void Version_flag_shows_the_version()
    {
        Assert.True(CliParser.Parse(["--version"]).ShowVersion);
    }
}
