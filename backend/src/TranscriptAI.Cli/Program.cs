using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptAI.Core;
using TranscriptAI.Core.Formatting;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Models;
using TranscriptAI.Core.Options;
using TranscriptAI.Core.Pipeline;
using TranscriptAI.Core.Sources;
using TranscriptAI.Core.Text;
using TranscriptAI.Core.YouTube;

namespace TranscriptAI.Cli;

/// <summary>
/// C# port of the Python <c>run.py</c>. Same flow and console messages:
/// create folders → get input → fetch &amp; sanitize the title → download/convert audio →
/// transcribe → write <c>Transcripts/&lt;title&gt;.txt</c> as <c>[start --&gt; end] text</c>.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var parsed = CliParser.Parse(args);
        if (parsed.ShowHelp)
        {
            Console.WriteLine(CliParser.HelpText);
            return 0;
        }

        if (parsed.ShowVersion)
        {
            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0");
            return 0;
        }

        if (parsed.Error is not null || parsed.Options is null)
        {
            WriteError(parsed.Error ?? "Invalid arguments.");
            Console.Error.WriteLine("Run 'transcript-ai --help' for usage.");
            return 2;
        }

        var options = parsed.Options;
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        await using var services = new ServiceCollection()
            .AddLogging(logging => logging
                .AddSimpleConsole(console =>
                {
                    console.SingleLine = true;
                    console.IncludeScopes = false;
                })
                .SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Warning))
            .AddTranscriptAI(BuildConfiguration(options))
            .BuildServiceProvider();

        try
        {
            return await RunAsync(options, services, cancellation.Token);
        }
        catch (TranscriptionException ex)
        {
            WriteError(ex.Message);
            return 1;
        }
        catch (OptionsValidationException ex)
        {
            WriteError(ex.Message);
            return 2;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            WriteError("Cancelled.");
            return 130;
        }
    }

    private static async Task<int> RunAsync(CliOptions options, IServiceProvider services, CancellationToken cancellationToken)
    {
        var pipeline = services.GetRequiredService<TranscriptPipeline>();
        var youtube = services.GetRequiredService<IYouTubeAudioDownloader>();
        var settings = services.GetRequiredService<IOptions<TranscriptionOptions>>().Value;

        if (!pipeline.Provider.IsConfigured)
        {
            WriteError($"{pipeline.Provider.Name} is not configured. Set the GROQ_API_KEY environment variable " +
                       "(free key: https://console.groq.com/keys) or HF_TOKEN for --provider huggingface.");
            return 2;
        }

        // -----------------------------
        // Create folders
        // -----------------------------
        var audioFolder = Path.GetFullPath(options.AudioDirectory);
        var transcriptFolder = Path.GetFullPath(options.TranscriptDirectory);
        Directory.CreateDirectory(audioFolder);
        Directory.CreateDirectory(transcriptFolder);

        // -----------------------------
        // Get the input (run.py: url = input("Enter YouTube video URL: "))
        // -----------------------------
        var input = options.Input;
        while (string.IsNullOrWhiteSpace(input))
        {
            Console.Write("Enter YouTube video URL or local file path: ");
            input = Console.ReadLine();
            if (input is null)
            {
                return 2; // stdin closed
            }
        }

        var source = MediaSource.Parse(input);
        string audioPath;
        string title;

        switch (source)
        {
            case MediaSource.YouTube video:
            {
                // Get video info and sanitize the title for Windows file names.
                var info = await youtube.GetInfoAsync(video.Url, cancellationToken);
                title = FileNameSanitizer.Sanitize(info.Title);
                var length = info.Duration is { } d ? $" ({d.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)})" : string.Empty;
                Console.WriteLine($"🎬 {info.Title} — {info.Author}{length}");

                // Download the audio-only stream (no FFmpeg needed for this step).
                var download = await youtube.DownloadAudioAsync(
                    video.Url,
                    audioFolder,
                    title,
                    preferredMaxBytes: pipeline.CanConvertMedia ? null : settings.MaxUploadBytes,
                    progress: new ConsoleProgressBar("Downloading audio"),
                    cancellationToken: cancellationToken);
                audioPath = download.Path;
                break;
            }

            case MediaSource.LocalFile file:
                title = FileNameSanitizer.Sanitize(Path.GetFileNameWithoutExtension(file.Path));
                audioPath = file.Path;
                Console.WriteLine($"🎧 Local file: {file.Path}");
                break;

            default:
                throw new UnreachableException();
        }

        if (!pipeline.CanConvertMedia)
        {
            Console.WriteLine($"ℹ️  FFmpeg not found — sending audio as-is (supported formats, up to {settings.MaxUploadMegabytes} MB). " +
                              "Use --ffmpeg or FFMPEG_PATH to enable conversion and chunking.");
        }

        // -----------------------------
        // Transcribe audio (hosted Whisper instead of whisper.load_model("base"))
        // -----------------------------
        var request = new TranscriptionRequest
        {
            Task = options.Translate ? TranscriptionTask.Translate : TranscriptionTask.Transcribe,
            Language = options.Language,
            Model = options.Model,
            Prompt = options.Prompt,
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await pipeline.TranscribeAsync(
            audioPath,
            title,
            request,
            new AudioPreparationRequest(audioFolder, title),
            new ConsoleStatus(),
            cancellationToken);

        if (result.PreparedAudioPath is { } prepared && !string.Equals(prepared, audioPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"\n✅ Audio downloaded and converted to MP3: {prepared}");
        }

        // -----------------------------
        // Save transcript(s)
        // -----------------------------
        var saved = new List<string>();
        foreach (var format in options.Formats)
        {
            var path = Path.Combine(transcriptFolder, title + TranscriptFormatter.FileExtension(format));
            var content = TranscriptFormatter.Format(result, format, Environment.NewLine);
            await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
            saved.Add(path);
        }

        var language = string.IsNullOrWhiteSpace(result.Language) ? "unknown" : result.Language;
        Console.WriteLine($"\n✅ Transcription complete (timestamped)! Saved as: {saved[0]}");
        foreach (var extra in saved.Skip(1))
        {
            Console.WriteLine($"   Also saved: {extra}");
        }

        Console.WriteLine($"   {result.Segments.Count} segments · language: {language} · {result.Provider} {result.Model} · " +
                          $"{stopwatch.Elapsed.TotalSeconds:F1}s{(result.ChunkCount > 1 ? $" · {result.ChunkCount} chunks" : string.Empty)}");
        return 0;
    }

    private static IConfiguration BuildConfiguration(CliOptions options)
    {
        var overrides = new Dictionary<string, string?>();
        if (options.Provider is not null)
        {
            overrides[$"{TranscriptionOptions.SectionName}:Provider"] = options.Provider;
        }

        if (options.FFmpegPath is not null)
        {
            overrides[$"{TranscriptionOptions.SectionName}:FFmpeg:Path"] = options.FFmpegPath;
        }

        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(overrides) // command-line flags win
            .Build();
    }

    private static void WriteError(string message)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"❌ {message}");
        Console.ForegroundColor = previous;
    }

    /// <summary>Prints pipeline status messages immediately (Progress&lt;T&gt; would post them asynchronously).</summary>
    private sealed class ConsoleStatus : IProgress<string>
    {
        public void Report(string value) => Console.WriteLine($"\n{value}");
    }

    private sealed class ConsoleProgressBar(string label) : IProgress<double>
    {
        private int _lastPercent = -1;

        public void Report(double value)
        {
            var percent = (int)Math.Clamp(Math.Round(value * 100), 0, 100);
            if (percent == _lastPercent || (Console.IsOutputRedirected && percent % 25 != 0))
            {
                return;
            }

            _lastPercent = percent;
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine($"{label}: {percent}%");
                return;
            }

            const int width = 30;
            var filled = percent * width / 100;
            Console.Write($"\r{label}: [{new string('█', filled)}{new string('░', width - filled)}] {percent,3}%");
            if (percent == 100)
            {
                Console.WriteLine();
            }
        }
    }
}
