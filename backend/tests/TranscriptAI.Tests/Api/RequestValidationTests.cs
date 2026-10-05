using TranscriptAI.Api;
using TranscriptAI.Core;
using TranscriptAI.Core.Models;

namespace TranscriptAI.Tests.Api;

public sealed class RequestValidationTests
{
    [Theory]
    [InlineData(null, TranscriptionTask.Transcribe)]
    [InlineData("", TranscriptionTask.Transcribe)]
    [InlineData("transcribe", TranscriptionTask.Transcribe)]
    [InlineData(" TRANSLATE ", TranscriptionTask.Translate)]
    public void Accepts_known_tasks(string? value, TranscriptionTask expected)
    {
        Assert.True(RequestValidation.TryParseTask(value, out var task));
        Assert.Equal(expected, task);
    }

    [Fact]
    public void Rejects_unknown_tasks()
    {
        Assert.False(RequestValidation.TryParseTask("summarize", out _));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("auto", null)]
    [InlineData(" AUTO ", null)]
    [InlineData("EN", "en")]
    [InlineData("ta", "ta")]
    [InlineData("yue", "yue")]
    public void Normalizes_language_codes(string? value, string? expected)
    {
        Assert.True(RequestValidation.TryNormalizeLanguage(value, out var language));
        Assert.Equal(expected, language);
    }

    [Theory]
    [InlineData("english")]
    [InlineData("e1")]
    [InlineData("en-US")]
    public void Rejects_anything_but_iso_codes(string value)
    {
        Assert.False(RequestValidation.TryNormalizeLanguage(value, out _));
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void Daily_quota_resets_at_midnight_utc()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 10, 5, 22, 0, 0, TimeSpan.Zero));
        var quota = new DailyQuota(Microsoft.Extensions.Options.Options.Create(new DemoOptions { DailyLimit = 2 }), clock);

        Assert.True(quota.TryConsume(out _));
        Assert.True(quota.TryConsume(out _));
        Assert.False(quota.TryConsume(out var resetIn));
        Assert.Equal(TimeSpan.FromHours(2), resetIn);

        clock.Now = clock.Now.AddHours(2);
        Assert.True(quota.TryConsume(out _));
    }

    [Theory]
    [InlineData(TranscriptionErrorKind.InvalidInput, 400)]
    [InlineData(TranscriptionErrorKind.NotSupported, 400)]
    [InlineData(TranscriptionErrorKind.UnsupportedMedia, 415)]
    [InlineData(TranscriptionErrorKind.TooLarge, 413)]
    [InlineData(TranscriptionErrorKind.MediaUnavailable, 422)]
    [InlineData(TranscriptionErrorKind.RateLimited, 429)]
    [InlineData(TranscriptionErrorKind.Upstream, 502)]
    [InlineData(TranscriptionErrorKind.Configuration, 503)]
    public void Maps_pipeline_errors_to_http_status_codes(TranscriptionErrorKind kind, int status)
    {
        Assert.Equal(status, TranscriptionExceptionHandler.Map(kind).Status);
    }
}
