using YoutubeExplode.Videos;

namespace TranscriptAI.Core.Sources;

/// <summary>What the user asked to transcribe.</summary>
public abstract record MediaSource
{
    private static readonly HashSet<string> YouTubeHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com",
        "youtu.be", "www.youtube-nocookie.com", "youtube-nocookie.com",
    };

    private MediaSource()
    {
    }

    public sealed record LocalFile(string Path) : MediaSource;

    public sealed record YouTube(string Url, string VideoId) : MediaSource;

    /// <summary>
    /// Classifies user input. Existing local paths win; otherwise the input must be a YouTube URL
    /// (watch, youtu.be, shorts, live, embed or music links).
    /// </summary>
    public static MediaSource Parse(string input, Func<string, bool>? fileExists = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        var trimmed = input.Trim().Trim('"');
        fileExists ??= File.Exists;

        if (fileExists(trimmed))
        {
            return new LocalFile(System.IO.Path.GetFullPath(trimmed));
        }

        if (TryParseYouTube(trimmed, out var youtube))
        {
            return youtube;
        }

        throw new TranscriptionException(
            $"'{trimmed}' is neither an existing file nor a YouTube video URL.",
            TranscriptionErrorKind.InvalidInput);
    }

    public static bool TryParseYouTube(string input, out YouTube result)
    {
        result = null!;
        if (!Uri.TryCreate(input?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !YouTubeHosts.Contains(uri.Host))
        {
            return false;
        }

        // YoutubeExplode only recognises youtube.* and youtu.be links, so other allow-listed hosts
        // (e.g. youtube-nocookie.com) are re-read as www.youtube.com with the same path and query.
        var candidate = uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase)
            ? uri.ToString()
            : "https://www.youtube.com" + uri.PathAndQuery;

        var videoId = YoutubeExplode.Videos.VideoId.TryParse(candidate);
        if (videoId is null)
        {
            return false;
        }

        // A canonical watch URL drops playlist, timestamp and tracking parameters.
        result = new YouTube($"https://www.youtube.com/watch?v={videoId.Value.Value}", videoId.Value.Value);
        return true;
    }
}
