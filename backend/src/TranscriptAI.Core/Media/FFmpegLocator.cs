using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptAI.Core.Options;

namespace TranscriptAI.Core.Media;

public interface IFFmpegLocator
{
    /// <summary>Full path to the ffmpeg executable, or <c>null</c> when FFmpeg is not installed.</summary>
    string? FindFFmpeg();
}

/// <summary>
/// Finds FFmpeg from configuration or PATH. Replaces the hard-coded
/// <c>E:\ffmpeg-8.0.1-full_build\bin</c> path from the Python script.
/// </summary>
public sealed class FFmpegLocator : IFFmpegLocator
{
    private readonly Lazy<string?> _path;

    public FFmpegLocator(IOptions<TranscriptionOptions> options, ILogger<FFmpegLocator> logger)
    {
        _path = new Lazy<string?>(() =>
        {
            var configured = options.Value.FFmpeg.Path;
            var found = Resolve(
                configured,
                Environment.GetEnvironmentVariable("PATH"),
                OperatingSystem.IsWindows(),
                File.Exists,
                Directory.Exists);

            if (found is null)
            {
                // A supported setup (e.g. App Service without FFmpeg), so not a warning.
                logger.LogInformation(
                    "FFmpeg was not found{Configured}. Files will be sent to the API as-is (supported formats, up to the upload limit).",
                    string.IsNullOrWhiteSpace(configured) ? string.Empty : $" at '{configured}' or on PATH");
            }
            else
            {
                logger.LogInformation("Using FFmpeg at {FFmpegPath}", found);
            }

            return found;
        });
    }

    public string? FindFFmpeg() => _path.Value;

    internal static string? Resolve(
        string? configuredPath,
        string? pathVariable,
        bool isWindows,
        Func<string, bool> fileExists,
        Func<string, bool> directoryExists)
    {
        var executable = isWindows ? "ffmpeg.exe" : "ffmpeg";

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            var trimmed = configuredPath.Trim().Trim('"');
            var candidate = directoryExists(trimmed) ? Path.Combine(trimmed, executable) : trimmed;
            if (fileExists(candidate))
            {
                return candidate;
            }
        }

        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), executable);
            if (fileExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
