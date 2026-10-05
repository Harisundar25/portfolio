using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptAI.Core.Options;

namespace TranscriptAI.Core.Media;

/// <summary>
/// Replaces the Python script's FFmpeg step (<c>FFmpegExtractAudio → mp3</c>).
/// With FFmpeg available, audio is extracted and downsampled to 16 kHz mono MP3 (Groq's
/// recommendation for speech) and split into chunks when it is still above the upload limit.
/// Without FFmpeg, supported files under the limit are passed through unchanged.
/// </summary>
public sealed partial class FFmpegAudioPreparer(
    IFFmpegLocator locator,
    IProcessRunner processRunner,
    IOptions<TranscriptionOptions> options,
    ILogger<FFmpegAudioPreparer> logger) : IAudioPreparer
{
    private const double ChunkSafetyFactor = 0.9;

    public bool CanConvert => locator.FindFFmpeg() is not null;

    public async Task<PreparedAudio> PrepareAsync(
        string inputPath,
        AudioPreparationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentNullException.ThrowIfNull(request);

        if (!File.Exists(inputPath))
        {
            throw new TranscriptionException($"Media file not found: {inputPath}", TranscriptionErrorKind.InvalidInput);
        }

        var settings = options.Value;
        var ffmpeg = locator.FindFFmpeg();
        if (ffmpeg is null)
        {
            return PassThrough(inputPath, settings.MaxUploadBytes);
        }

        Directory.CreateDirectory(request.WorkingDirectory);
        var outputPath = Path.Combine(request.WorkingDirectory, request.OutputBaseName + ".mp3");
        if (string.Equals(Path.GetFullPath(outputPath), Path.GetFullPath(inputPath), StringComparison.OrdinalIgnoreCase))
        {
            outputPath = Path.Combine(request.WorkingDirectory, request.OutputBaseName + ".16k.mp3");
        }

        logger.LogInformation("Converting {Input} to 16 kHz mono MP3", Path.GetFileName(inputPath));
        var conversion = await processRunner.RunAsync(
            ffmpeg,
            [
                "-hide_banner", "-nostdin", "-y",
                "-i", inputPath,
                "-vn", "-map", "0:a:0",
                "-ac", "1", "-ar", "16000",
                "-c:a", "libmp3lame", "-b:a", $"{settings.AudioBitrateKbps}k",
                outputPath,
            ],
            cancellationToken);

        if (conversion.ExitCode != 0 || !File.Exists(outputPath))
        {
            throw new TranscriptionException(
                $"FFmpeg could not extract audio from this file. {Summarize(conversion.StandardError)}",
                TranscriptionErrorKind.MediaUnavailable);
        }

        var totalDuration = ParseDuration(conversion.StandardError);
        var outputSize = new FileInfo(outputPath).Length;

        if (outputSize <= settings.MaxUploadBytes)
        {
            return new PreparedAudio([new AudioChunk(outputPath, 0, totalDuration)], totalDuration, outputPath);
        }

        // Still too large (very long recording): split into equal chunks below the limit.
        var durationForSplit = totalDuration ?? outputSize * 8.0 / (settings.AudioBitrateKbps * 1000);
        var chunkSeconds = ComputeChunkSeconds(outputSize, durationForSplit, settings.MaxUploadBytes);
        var chunkDirectory = Path.Combine(request.WorkingDirectory, $".chunks-{Guid.NewGuid():N}");
        Directory.CreateDirectory(chunkDirectory);

        logger.LogInformation(
            "Audio is {SizeMb:F1} MB; splitting into ~{ChunkSeconds}s chunks",
            outputSize / 1_000_000.0,
            chunkSeconds);

        var split = await processRunner.RunAsync(
            ffmpeg,
            [
                "-hide_banner", "-nostdin", "-y",
                "-i", outputPath,
                "-f", "segment",
                "-segment_time", chunkSeconds.ToString(CultureInfo.InvariantCulture),
                "-c", "copy",
                "-reset_timestamps", "1",
                Path.Combine(chunkDirectory, "chunk_%03d.mp3"),
            ],
            cancellationToken);

        var chunkFiles = Directory.GetFiles(chunkDirectory, "chunk_*.mp3").Order(StringComparer.Ordinal).ToArray();
        if (split.ExitCode != 0 || chunkFiles.Length == 0)
        {
            TryDeleteDirectory(chunkDirectory);
            throw new TranscriptionException(
                $"FFmpeg could not split the audio into chunks. {Summarize(split.StandardError)}",
                TranscriptionErrorKind.MediaUnavailable);
        }

        // Probe each chunk so segment timestamps can be offset precisely.
        var chunks = new List<AudioChunk>(chunkFiles.Length);
        var offset = 0d;
        foreach (var file in chunkFiles)
        {
            var probe = await processRunner.RunAsync(ffmpeg, ["-hide_banner", "-nostdin", "-i", file], cancellationToken);
            var chunkDuration = ParseDuration(probe.StandardError) ?? chunkSeconds;
            chunks.Add(new AudioChunk(file, offset, chunkDuration));
            offset += chunkDuration;
        }

        return new PreparedAudio(chunks, totalDuration ?? offset, outputPath, [chunkDirectory]);
    }

    /// <summary>No FFmpeg: send the original file if the API can take it.</summary>
    internal static PreparedAudio PassThrough(string inputPath, long maxUploadBytes)
    {
        var extension = Path.GetExtension(inputPath);
        if (!AudioFormats.IsDirectlySupported(inputPath))
        {
            throw new TranscriptionException(
                $"'{extension}' files must be converted first, but FFmpeg isn't installed. " +
                $"Use one of: {string.Join(", ", AudioFormats.DirectUploadExtensions)} — or install FFmpeg.",
                TranscriptionErrorKind.UnsupportedMedia);
        }

        var size = new FileInfo(inputPath).Length;
        if (size > maxUploadBytes)
        {
            throw new TranscriptionException(
                $"The file is {size / 1_000_000.0:F1} MB but the API accepts at most {maxUploadBytes / 1_000_000.0:F0} MB per request. " +
                "Install FFmpeg so long recordings can be downsampled and split automatically.",
                TranscriptionErrorKind.TooLarge);
        }

        return new PreparedAudio([new AudioChunk(inputPath, 0, null)], null, convertedPath: null);
    }

    internal static int ComputeChunkSeconds(long fileBytes, double durationSeconds, long maxUploadBytes)
    {
        var bytesPerSecond = fileBytes / Math.Max(durationSeconds, 1);
        var seconds = (int)Math.Floor(maxUploadBytes * ChunkSafetyFactor / bytesPerSecond);
        return Math.Clamp(seconds, 30, 3600);
    }

    [GeneratedRegex(@"Duration:\s*(\d+):(\d{2}):(\d{2}(?:\.\d+)?)")]
    private static partial Regex DurationPattern();

    /// <summary>Reads "Duration: HH:MM:SS.ss" from FFmpeg's log output.</summary>
    internal static double? ParseDuration(string ffmpegOutput)
    {
        var match = DurationPattern().Match(ffmpegOutput ?? string.Empty);
        if (!match.Success)
        {
            return null;
        }

        var hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        return hours * 3600 + minutes * 60 + seconds;
    }

    private static string Summarize(string standardError)
    {
        var lastLine = (standardError ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
        return lastLine is null ? string.Empty : $"FFmpeg said: {lastLine}";
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
