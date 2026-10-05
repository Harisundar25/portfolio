namespace TranscriptAI.Api;

/// <summary>
/// Guard rails for the public demo, bound from the <c>Demo</c> section
/// (e.g. <c>Demo__DailyLimit=100</c> as an App Service setting).
/// Defaults suit the App Service Free (F1) tier, whose 165 MB/day outbound quota
/// also covers the audio this API forwards to Groq.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public bool AllowYouTube { get; set; } = true;

    /// <summary>Longest YouTube video accepted, protecting the free audio-seconds quota.</summary>
    public int MaxDurationMinutes { get; set; } = 15;

    public int MaxUploadMegabytes { get; set; } = 10;

    /// <summary>Requests per client IP per window.</summary>
    public int PermitLimitPerWindow { get; set; } = 5;

    public int WindowMinutes { get; set; } = 10;

    /// <summary>Transcriptions started per UTC day across all visitors (in memory; also resets on restart).</summary>
    public int DailyLimit { get; set; } = 40;

    public int MaxConcurrentJobs { get; set; } = 2;

    public long MaxUploadBytes => MaxUploadMegabytes * 1_000_000L;

    /// <summary>Upload limit plus headroom for multipart framing.</summary>
    public long MaxRequestBytes => MaxUploadBytes + 1_000_000L;
}
