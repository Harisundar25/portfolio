using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos.Streams;

namespace TranscriptAI.Core.YouTube;

public sealed record YouTubeVideoInfo(string Id, string Title, string Author, TimeSpan? Duration);

public sealed record DownloadedAudio(string Path, long SizeBytes, long BitsPerSecond);

/// <summary>Replaces yt-dlp from the Python script.</summary>
public interface IYouTubeAudioDownloader
{
    Task<YouTubeVideoInfo> GetInfoAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an audio-only stream to <paramref name="outputDirectory"/>.
    /// <paramref name="preferredMaxBytes"/> picks a stream small enough to upload without FFmpeg when possible.
    /// </summary>
    Task<DownloadedAudio> DownloadAudioAsync(
        string url,
        string outputDirectory,
        string fileNameWithoutExtension,
        long? preferredMaxBytes,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class YoutubeExplodeAudioDownloader(YoutubeClient client, ILogger<YoutubeExplodeAudioDownloader> logger)
    : IYouTubeAudioDownloader
{
    public async Task<YouTubeVideoInfo> GetInfoAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var video = await client.Videos.GetAsync(url, cancellationToken);
            return new YouTubeVideoInfo(video.Id.Value, video.Title, video.Author.ChannelTitle, video.Duration);
        }
        catch (YoutubeExplodeException ex)
        {
            throw Translate(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new TranscriptionException("Couldn't reach YouTube. Please try again.", TranscriptionErrorKind.Upstream, ex);
        }
    }

    public async Task<DownloadedAudio> DownloadAudioAsync(
        string url,
        string outputDirectory,
        string fileNameWithoutExtension,
        long? preferredMaxBytes,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var manifest = await client.Videos.Streams.GetManifestAsync(url, cancellationToken);
            var streams = manifest.GetAudioOnlyStreams().ToList();
            var candidates = streams
                .Select((s, i) => new AudioStreamCandidate(i, s.Size.Bytes, s.Bitrate.BitsPerSecond, s.Container.Name))
                .ToList();

            var choice = AudioStreamSelector.Select(candidates, preferredMaxBytes)
                ?? throw new TranscriptionException(
                    "This video has no downloadable audio stream.",
                    TranscriptionErrorKind.MediaUnavailable);
            var stream = streams[choice.Index];

            // Audio-only MP4 is saved as .m4a; WebM (Opus) keeps .webm. Both are accepted by the Whisper API.
            var extension = stream.Container == Container.Mp4 ? ".m4a" : "." + stream.Container.Name;
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, fileNameWithoutExtension + extension);

            logger.LogInformation(
                "Downloading {Container} audio ({Kbps} kbps, {SizeMb:F1} MB)",
                stream.Container.Name,
                stream.Bitrate.KiloBitsPerSecond.ToString("F0", System.Globalization.CultureInfo.InvariantCulture),
                stream.Size.MegaBytes);

            await client.Videos.Streams.DownloadAsync(stream, path, progress, cancellationToken);
            return new DownloadedAudio(path, stream.Size.Bytes, stream.Bitrate.BitsPerSecond);
        }
        catch (YoutubeExplodeException ex)
        {
            throw Translate(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new TranscriptionException("Downloading audio from YouTube failed. Please try again.", TranscriptionErrorKind.Upstream, ex);
        }
    }

    private static TranscriptionException Translate(YoutubeExplodeException ex) => ex switch
    {
        VideoRequiresPurchaseException => new("This video requires purchase, so its audio can't be downloaded.", TranscriptionErrorKind.MediaUnavailable, ex),
        VideoUnavailableException => new("This video is unavailable (private, deleted or region-locked).", TranscriptionErrorKind.MediaUnavailable, ex),
        VideoUnplayableException => new(
            "YouTube refused to serve this video. This often happens for age-restricted videos or requests from cloud servers — " +
            "try uploading the audio file instead, or run the CLI locally.",
            TranscriptionErrorKind.MediaUnavailable,
            ex),
        RequestLimitExceededException => new("YouTube is rate limiting requests right now. Please try again later.", TranscriptionErrorKind.Upstream, ex),
        _ => new($"YouTube error: {ex.Message}", TranscriptionErrorKind.Upstream, ex),
    };
}
