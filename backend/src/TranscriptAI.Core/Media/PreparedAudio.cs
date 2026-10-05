namespace TranscriptAI.Core.Media;

/// <summary>A file ready to upload, positioned at <see cref="OffsetSeconds"/> within the original recording.</summary>
public sealed record AudioChunk(string Path, double OffsetSeconds, double? DurationSeconds);

/// <summary>Where and how to write prepared audio.</summary>
/// <param name="WorkingDirectory">Folder for the converted MP3 and any chunk files.</param>
/// <param name="OutputBaseName">File name (without extension) for the converted MP3.</param>
public sealed record AudioPreparationRequest(string WorkingDirectory, string OutputBaseName);

/// <summary>
/// Audio ready for transcription. Disposing deletes temporary chunk files
/// (the converted MP3 itself is kept, like the Python script's AudioFiles/ output).
/// </summary>
public sealed class PreparedAudio(
    IReadOnlyList<AudioChunk> chunks,
    double? totalDurationSeconds,
    string? convertedPath,
    IReadOnlyList<string>? temporaryPaths = null) : IDisposable
{
    private readonly IReadOnlyList<string> _temporaryPaths = temporaryPaths ?? [];

    public IReadOnlyList<AudioChunk> Chunks { get; } = chunks;

    public double? TotalDurationSeconds { get; } = totalDurationSeconds;

    /// <summary>Path of the FFmpeg-converted MP3, or <c>null</c> when the original file was used as-is.</summary>
    public string? ConvertedPath { get; } = convertedPath;

    public void Dispose()
    {
        foreach (var path in _temporaryPaths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort cleanup.
            }
        }
    }
}

/// <summary>Turns any input media into one or more provider-ready audio files.</summary>
public interface IAudioPreparer
{
    /// <summary>True when FFmpeg is available for conversion and chunking.</summary>
    bool CanConvert { get; }

    Task<PreparedAudio> PrepareAsync(string inputPath, AudioPreparationRequest request, CancellationToken cancellationToken = default);
}
