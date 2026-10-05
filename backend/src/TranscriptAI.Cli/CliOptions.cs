using System.Text.RegularExpressions;
using TranscriptAI.Core.Formatting;

namespace TranscriptAI.Cli;

/// <summary>Parsed command-line options.</summary>
internal sealed record CliOptions
{
    /// <summary>YouTube URL or local media path. When omitted the user is prompted, like run.py.</summary>
    public string? Input { get; init; }

    public string? Language { get; init; }

    public bool Translate { get; init; }

    public string? Model { get; init; }

    public string? Prompt { get; init; }

    public IReadOnlyList<TranscriptFormat> Formats { get; init; } = [TranscriptFormat.Txt];

    public string TranscriptDirectory { get; init; } = "Transcripts";

    public string AudioDirectory { get; init; } = "AudioFiles";

    public string? FFmpegPath { get; init; }

    /// <summary>"Groq" or "HuggingFace" (configuration enum names).</summary>
    public string? Provider { get; init; }

    public bool Verbose { get; init; }
}

internal sealed record CliParseResult(CliOptions? Options, string? Error = null, bool ShowHelp = false, bool ShowVersion = false);

internal static partial class CliParser
{
    public const string HelpText = """
        transcript-ai — turn a YouTube video or a local audio/video file into a timestamped transcript.
        C# port of the Python Transcript-AI script; inference runs on open-source Whisper models via Groq (free API key).

        Usage:
          transcript-ai [<youtube-url-or-file>] [options]

        Options:
          -l, --language <code>    Source language (ISO-639-1, e.g. en, ta, hi). Default: auto-detect
          -t, --translate          Translate the speech to English (uses whisper-large-v3)
          -m, --model <id>         Override the model (default: whisper-large-v3-turbo)
          -f, --format <list>      txt, srt, vtt, json or all — comma separated (default: txt)
          -o, --output <dir>       Transcript folder (default: Transcripts)
          -a, --audio-dir <dir>    Audio folder (default: AudioFiles)
              --ffmpeg <path>      FFmpeg executable or its folder (default: search PATH)
          -p, --provider <name>    groq (default) or huggingface
              --prompt <text>      Context or spelling hints for the model
          -v, --verbose            Detailed logging
          -h, --help               Show this help
              --version            Show the version

        Environment:
          GROQ_API_KEY             Free key from https://console.groq.com/keys
          HF_TOKEN                 Hugging Face token (only for --provider huggingface)
          FFMPEG_PATH              Alternative to --ffmpeg

        Examples:
          transcript-ai "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
          transcript-ai lecture.mp4 --language ta --format txt,srt
          transcript-ai interview.mp3 --translate
        """;

    [GeneratedRegex("^[A-Za-z]{2,3}$")]
    private static partial Regex LanguageCode();

    public static CliParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var options = new CliOptions();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            string? inlineValue = null;
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Contains('='))
            {
                var split = arg.IndexOf('=');
                inlineValue = arg[(split + 1)..];
                arg = arg[..split];
            }

            string? Value()
            {
                if (inlineValue is not null)
                {
                    return inlineValue;
                }

                return i + 1 < args.Count ? args[++i] : null;
            }

            switch (arg)
            {
                case "-h" or "--help" or "/?":
                    return new CliParseResult(options, ShowHelp: true);
                case "--version":
                    return new CliParseResult(options, ShowVersion: true);
                case "-t" or "--translate":
                    options = options with { Translate = true };
                    break;
                case "-v" or "--verbose":
                    options = options with { Verbose = true };
                    break;
                case "-l" or "--language":
                {
                    var value = Value();
                    if (value is null || !LanguageCode().IsMatch(value))
                    {
                        return Fail($"'{arg}' expects an ISO-639-1 language code such as 'en' or 'ta'.");
                    }

                    options = options with { Language = value.ToLowerInvariant() };
                    break;
                }

                case "-m" or "--model":
                {
                    var value = Value();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return Fail($"'{arg}' expects a model id.");
                    }

                    options = options with { Model = value };
                    break;
                }

                case "--prompt":
                {
                    var value = Value();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return Fail("'--prompt' expects some text.");
                    }

                    options = options with { Prompt = value };
                    break;
                }

                case "-f" or "--format":
                {
                    var formats = ParseFormats(Value());
                    if (formats is null)
                    {
                        return Fail($"'{arg}' expects txt, srt, vtt, json or all (comma separated).");
                    }

                    options = options with { Formats = formats };
                    break;
                }

                case "-o" or "--output":
                {
                    var value = Value();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return Fail($"'{arg}' expects a folder path.");
                    }

                    options = options with { TranscriptDirectory = value };
                    break;
                }

                case "-a" or "--audio-dir":
                {
                    var value = Value();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return Fail($"'{arg}' expects a folder path.");
                    }

                    options = options with { AudioDirectory = value };
                    break;
                }

                case "--ffmpeg":
                {
                    var value = Value();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        return Fail("'--ffmpeg' expects the FFmpeg executable or folder.");
                    }

                    options = options with { FFmpegPath = value };
                    break;
                }

                case "-p" or "--provider":
                {
                    var provider = Value()?.Trim().ToLowerInvariant() switch
                    {
                        "groq" => "Groq",
                        "huggingface" or "hugging-face" or "hf" => "HuggingFace",
                        _ => null,
                    };
                    if (provider is null)
                    {
                        return Fail($"'{arg}' expects 'groq' or 'huggingface'.");
                    }

                    options = options with { Provider = provider };
                    break;
                }

                default:
                    if (arg.StartsWith('-') && arg.Length > 1)
                    {
                        return Fail($"Unknown option '{arg}'.");
                    }

                    if (options.Input is not null)
                    {
                        return Fail("Only one URL or file can be transcribed at a time.");
                    }

                    options = options with { Input = arg };
                    break;
            }
        }

        return new CliParseResult(options);
    }

    internal static IReadOnlyList<TranscriptFormat>? ParseFormats(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return [TranscriptFormat.Txt, TranscriptFormat.Srt, TranscriptFormat.Vtt, TranscriptFormat.Json];
        }

        var formats = new List<TranscriptFormat>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!part.All(char.IsLetter) ||
                !Enum.TryParse<TranscriptFormat>(part, ignoreCase: true, out var format) ||
                !Enum.IsDefined(format))
            {
                return null;
            }

            if (!formats.Contains(format))
            {
                formats.Add(format);
            }
        }

        return formats.Count > 0 ? formats : null;
    }

    private static CliParseResult Fail(string message) => new(null, message);
}
