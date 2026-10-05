namespace TranscriptAI.Core.Media;

/// <summary>Media formats understood by the Whisper APIs and by FFmpeg.</summary>
public static class AudioFormats
{
    // Formats the Groq / OpenAI-compatible Whisper endpoints accept as-is.
    private static readonly Dictionary<string, string> DirectUploadTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".flac"] = "audio/flac",
        [".m4a"] = "audio/mp4",
        [".mp3"] = "audio/mpeg",
        [".mp4"] = "video/mp4",
        [".mpeg"] = "audio/mpeg",
        [".mpga"] = "audio/mpeg",
        [".ogg"] = "audio/ogg",
        [".wav"] = "audio/wav",
        [".webm"] = "audio/webm",
    };

    // Extra containers that need FFmpeg to extract/convert the audio track first.
    private static readonly HashSet<string> ConvertibleOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        ".3gp", ".aac", ".aiff", ".amr", ".avi", ".flv", ".m4v", ".mkv", ".mov", ".mts", ".opus", ".ts", ".wma", ".wmv",
    };

    public static IReadOnlyCollection<string> DirectUploadExtensions => DirectUploadTypes.Keys;

    public static IReadOnlyCollection<string> AllExtensions =>
        [.. DirectUploadTypes.Keys, .. ConvertibleOnly];

    /// <summary>True if the provider accepts this file without conversion.</summary>
    public static bool IsDirectlySupported(string path) => DirectUploadTypes.ContainsKey(Path.GetExtension(path));

    /// <summary>True if the file can be processed when FFmpeg is available.</summary>
    public static bool IsKnownMediaFile(string path)
    {
        var extension = Path.GetExtension(path);
        return DirectUploadTypes.ContainsKey(extension) || ConvertibleOnly.Contains(extension);
    }

    public static string GetContentType(string path) =>
        DirectUploadTypes.TryGetValue(Path.GetExtension(path), out var type) ? type : "application/octet-stream";
}
