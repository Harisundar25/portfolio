using Microsoft.Extensions.Logging.Abstractions;
using TranscriptAI.Core;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Sources;
using TranscriptAI.Core.YouTube;

namespace TranscriptAI.Tests.Core;

public sealed class MediaSourceTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&list=PL123&t=42s")]
    [InlineData("http://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://music.youtube.com/watch?v=dQw4w9WgXcQ&feature=share")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    public void Recognises_youtube_links_and_canonicalises_them(string url)
    {
        Assert.True(MediaSource.TryParseYouTube(url, out var video));
        Assert.Equal("dQw4w9WgXcQ", video.VideoId);
        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", video.Url);
    }

    [Theory]
    [InlineData("https://evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("ftp://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?list=PL123")]
    [InlineData("https://www.youtube.com/@channel")]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("not a url")]
    public void Rejects_anything_that_is_not_a_youtube_video(string input)
    {
        Assert.False(MediaSource.TryParseYouTube(input, out _));
    }

    [Fact]
    public void Existing_local_files_win_and_quotes_are_trimmed()
    {
        var path = Path.Combine(Path.GetTempPath(), "talk.mp3");

        var source = MediaSource.Parse($"\"{path}\"", candidate => candidate == path);

        Assert.Equal(new MediaSource.LocalFile(Path.GetFullPath(path)), source);
    }

    [Fact]
    public void Unknown_input_is_rejected_with_a_helpful_message()
    {
        var error = Assert.Throws<TranscriptionException>(() => MediaSource.Parse("missing.mp3", _ => false));

        Assert.Equal(TranscriptionErrorKind.InvalidInput, error.Kind);
        Assert.Contains("neither an existing file nor a YouTube video URL", error.Message);
    }
}

public sealed class AudioStreamSelectorTests
{
    private static readonly AudioStreamCandidate Low = new(0, 400_000, 32_000, "webm");
    private static readonly AudioStreamCandidate Speech = new(1, 1_000_000, 64_000, "mp4");
    private static readonly AudioStreamCandidate High = new(2, 2_500_000, 160_000, "webm");

    [Fact]
    public void Prefers_the_smallest_stream_good_enough_for_speech()
    {
        Assert.Equal(Speech, AudioStreamSelector.Select([High, Low, Speech], maxBytes: null));
    }

    [Fact]
    public void Falls_back_to_the_best_stream_when_all_are_low_bitrate()
    {
        var tiny = Low with { Index = 3, SizeBytes = 300_000, BitsPerSecond = 24_000 };
        Assert.Equal(Low, AudioStreamSelector.Select([tiny, Low], maxBytes: null));
    }

    [Fact]
    public void Respects_the_upload_limit_when_ffmpeg_cannot_split()
    {
        Assert.Equal(Low, AudioStreamSelector.Select([High, Low, Speech], maxBytes: 500_000));
        Assert.Equal(Low, AudioStreamSelector.Select([High, Low, Speech], maxBytes: 100)); // nothing fits: smallest
    }

    [Fact]
    public void Returns_null_without_streams()
    {
        Assert.Null(AudioStreamSelector.Select([], maxBytes: null));
    }
}

public sealed class FFmpegLocatorTests
{
    private static readonly string Tools = Path.Combine(Path.GetTempPath(), "tools");

    [Fact]
    public void Configured_folder_resolves_to_the_executable_inside_it()
    {
        var folder = Path.Combine(Tools, "ffmpeg", "bin");
        var exe = Path.Combine(folder, "ffmpeg.exe");

        var found = FFmpegLocator.Resolve($"\"{folder}\"", pathVariable: null, isWindows: true, p => p == exe, p => p == folder);

        Assert.Equal(exe, found);
    }

    [Fact]
    public void Configured_executable_path_is_used_as_is()
    {
        var exe = Path.Combine(Tools, "ffmpeg");

        Assert.Equal(exe, FFmpegLocator.Resolve(exe, pathVariable: null, isWindows: false, p => p == exe, _ => false));
    }

    [Fact]
    public void Searches_path_when_not_configured_or_missing()
    {
        var first = Path.Combine(Tools, "a");
        var second = Path.Combine(Tools, "b");
        var exe = Path.Combine(second, "ffmpeg");
        var path = string.Join(Path.PathSeparator, first, second);

        Assert.Equal(exe, FFmpegLocator.Resolve(null, path, isWindows: false, p => p == exe, _ => false));
        Assert.Equal(exe, FFmpegLocator.Resolve(Path.Combine(Tools, "missing"), path, isWindows: false, p => p == exe, _ => false));
    }

    [Fact]
    public void Returns_null_when_ffmpeg_is_not_installed()
    {
        Assert.Null(FFmpegLocator.Resolve(null, Tools, isWindows: true, _ => false, _ => false));
        Assert.Null(FFmpegLocator.Resolve(null, null, isWindows: true, _ => true, _ => false));
    }
}

public sealed class FFmpegAudioPreparerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static FFmpegAudioPreparer Create(string? ffmpeg, IProcessRunner runner, int maxUploadMegabytes = 25) =>
        new(
            new StubFFmpegLocator(ffmpeg),
            runner,
            TestOptions.Create(o => o.MaxUploadMegabytes = maxUploadMegabytes),
            NullLogger<FFmpegAudioPreparer>.Instance);

    private static FakeProcessRunner Unused() =>
        new(_ => throw new InvalidOperationException("FFmpeg should not run."));

    [Theory]
    [InlineData("Input #0, mp3\n  Duration: 01:02:03.45, start: 0.025057, bitrate: 128 kb/s", 3723.45)]
    [InlineData("  Duration: 00:00:07.00, start", 7)]
    public void Reads_the_duration_from_ffmpeg_output(string output, double expected)
    {
        Assert.Equal(expected, FFmpegAudioPreparer.ParseDuration(output));
    }

    [Fact]
    public void Missing_duration_is_null()
    {
        Assert.Null(FFmpegAudioPreparer.ParseDuration("Duration: N/A"));
    }

    [Theory]
    [InlineData(50_000_000L, 1000d, 25_000_000L, 450)]
    [InlineData(10_000_000_000L, 10d, 25_000_000L, 30)]
    [InlineData(1_000_000L, 100_000d, 25_000_000L, 3600)]
    public void Chunk_length_keeps_each_chunk_under_the_limit(long bytes, double seconds, long limit, int expected)
    {
        Assert.Equal(expected, FFmpegAudioPreparer.ComputeChunkSeconds(bytes, seconds, limit));
    }

    [Fact]
    public async Task Without_ffmpeg_supported_files_are_sent_as_is()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("note.m4a", 2048);

        using var prepared = await Create(null, Unused()).PrepareAsync(input, new AudioPreparationRequest(temp.Path, "note"), Ct);

        var chunk = Assert.Single(prepared.Chunks);
        Assert.Equal(input, chunk.Path);
        Assert.Null(prepared.ConvertedPath);
    }

    [Fact]
    public async Task Without_ffmpeg_other_containers_are_rejected()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("movie.mkv");

        var error = await Assert.ThrowsAsync<TranscriptionException>(
            () => Create(null, Unused()).PrepareAsync(input, new AudioPreparationRequest(temp.Path, "movie"), Ct));

        Assert.Equal(TranscriptionErrorKind.UnsupportedMedia, error.Kind);
    }

    [Fact]
    public async Task Without_ffmpeg_files_over_the_limit_are_rejected()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("long.mp3", 1_500_000);

        var error = await Assert.ThrowsAsync<TranscriptionException>(
            () => Create(null, Unused(), maxUploadMegabytes: 1).PrepareAsync(input, new AudioPreparationRequest(temp.Path, "long"), Ct));

        Assert.Equal(TranscriptionErrorKind.TooLarge, error.Kind);
    }

    [Fact]
    public async Task Missing_input_is_invalid()
    {
        using var temp = new TempDirectory();

        var error = await Assert.ThrowsAsync<TranscriptionException>(
            () => Create(null, Unused()).PrepareAsync(Path.Combine(temp.Path, "nope.mp3"), new AudioPreparationRequest(temp.Path, "x"), Ct));

        Assert.Equal(TranscriptionErrorKind.InvalidInput, error.Kind);
    }

    [Fact]
    public async Task Converts_to_16_khz_mono_mp3()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("talk.mp3", 4096);
        var runner = new FakeProcessRunner(args =>
        {
            File.WriteAllBytes(args[^1], new byte[2048]); // FFmpeg writes the output file (last argument)
            return new ProcessResult(0, string.Empty, "Duration: 00:02:03.50, start: 0.000000");
        });

        using var prepared = await Create("ffmpeg", runner).PrepareAsync(input, new AudioPreparationRequest(temp.Path, "talk"), Ct);

        // The input already is talk.mp3 in the same folder, so the output must not overwrite it.
        var expectedOutput = Path.Combine(temp.Path, "talk.16k.mp3");
        var chunk = Assert.Single(prepared.Chunks);
        Assert.Equal(new AudioChunk(expectedOutput, 0, 123.5), chunk);
        Assert.Equal(expectedOutput, prepared.ConvertedPath);
        Assert.Equal(123.5, prepared.TotalDurationSeconds);

        var args = Assert.Single(runner.Calls).ToList();
        Assert.Equal("1", args[args.IndexOf("-ac") + 1]);
        Assert.Equal("16000", args[args.IndexOf("-ar") + 1]);
        Assert.Equal("32k", args[args.IndexOf("-b:a") + 1]);
        Assert.Contains("-vn", args);
    }

    [Fact]
    public async Task Splits_audio_that_is_still_over_the_limit_and_cleans_up_chunks()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("lecture.mp4", 128);
        var runner = new FakeProcessRunner(args =>
        {
            if (args.Contains("segment"))
            {
                var folder = Path.GetDirectoryName(args[^1])!;
                for (var i = 0; i < 3; i++)
                {
                    File.WriteAllBytes(Path.Combine(folder, $"chunk_{i:000}.mp3"), new byte[10]);
                }

                return new ProcessResult(0, string.Empty, string.Empty);
            }

            if (args.Count == 4) // probe: -hide_banner -nostdin -i <chunk>
            {
                var last = args[^1].EndsWith("chunk_002.mp3", StringComparison.Ordinal);
                return new ProcessResult(1, string.Empty, last ? "Duration: 00:01:00.00," : "Duration: 00:05:00.00,");
            }

            File.WriteAllBytes(args[^1], new byte[2_400_000]); // 2.4 MB > 1 MB limit
            return new ProcessResult(0, string.Empty, "Duration: 00:11:00.00, start");
        });

        string chunkFolder;
        using (var prepared = await Create("ffmpeg", runner, maxUploadMegabytes: 1)
                   .PrepareAsync(input, new AudioPreparationRequest(temp.Path, "lecture"), Ct))
        {
            Assert.Equal(new[] { 0d, 300d, 600d }, prepared.Chunks.Select(c => c.OffsetSeconds));
            Assert.Equal(660, prepared.TotalDurationSeconds);

            // 1 MB × 0.9 safety factor ÷ (2.4 MB / 660 s) ≈ 247 s per chunk.
            var split = runner.Calls.Single(c => c.Contains("segment")).ToList();
            Assert.Equal("247", split[split.IndexOf("-segment_time") + 1]);

            chunkFolder = Path.GetDirectoryName(prepared.Chunks[0].Path)!;
            Assert.True(Directory.Exists(chunkFolder));
        }

        Assert.False(Directory.Exists(chunkFolder));
        Assert.True(File.Exists(Path.Combine(temp.Path, "lecture.mp3"))); // the converted MP3 is kept
    }

    [Fact]
    public async Task Reports_ffmpeg_failures_with_its_last_message()
    {
        using var temp = new TempDirectory();
        var input = temp.CreateFile("broken.mp4");
        var runner = new FakeProcessRunner(_ => new ProcessResult(1, string.Empty, "Input #0\nInvalid data found when processing input\n"));

        var error = await Assert.ThrowsAsync<TranscriptionException>(
            () => Create("ffmpeg", runner).PrepareAsync(input, new AudioPreparationRequest(temp.Path, "broken"), Ct));

        Assert.Equal(TranscriptionErrorKind.MediaUnavailable, error.Kind);
        Assert.EndsWith("FFmpeg said: Invalid data found when processing input", error.Message);
    }
}
