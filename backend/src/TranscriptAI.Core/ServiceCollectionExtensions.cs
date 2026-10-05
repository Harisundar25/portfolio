using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TranscriptAI.Core.Media;
using TranscriptAI.Core.Options;
using TranscriptAI.Core.Pipeline;
using TranscriptAI.Core.Transcription;
using TranscriptAI.Core.YouTube;
using YoutubeExplode;

namespace TranscriptAI.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the transcription pipeline, providers, FFmpeg and YouTube services.</summary>
    public static IServiceCollection AddTranscriptAI(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TranscriptionOptions>()
            .Bind(configuration.GetSection(TranscriptionOptions.SectionName))
            .PostConfigure(o =>
            {
                // Conventional environment variables as a fallback (handy for the CLI).
                if (string.IsNullOrWhiteSpace(o.Groq.ApiKey))
                {
                    o.Groq.ApiKey = configuration["GROQ_API_KEY"];
                }

                if (string.IsNullOrWhiteSpace(o.HuggingFace.ApiKey))
                {
                    o.HuggingFace.ApiKey = configuration["HF_TOKEN"];
                }

                if (string.IsNullOrWhiteSpace(o.FFmpeg.Path))
                {
                    o.FFmpeg.Path = configuration["FFMPEG_PATH"];
                }
            })
            .Validate(o => o.MaxUploadMegabytes is > 0 and <= 100, "Transcription:MaxUploadMegabytes must be between 1 and 100.")
            .Validate(o => o.AudioBitrateKbps is >= 16 and <= 192, "Transcription:AudioBitrateKbps must be between 16 and 192.")
            .Validate(o => o.RequestTimeoutSeconds is >= 10 and <= 900, "Transcription:RequestTimeoutSeconds must be between 10 and 900.")
            .Validate(o => o.Retry.MaxRetries is >= 0 and <= 10, "Transcription:Retry:MaxRetries must be between 0 and 10.");

        services.TryAddSingleton<IProcessRunner, ProcessRunner>();
        services.TryAddSingleton<IFFmpegLocator, FFmpegLocator>();
        services.TryAddSingleton<IAudioPreparer, FFmpegAudioPreparer>();

        services.TryAddSingleton(_ => new YoutubeClient());
        services.TryAddSingleton<IYouTubeAudioDownloader, YoutubeExplodeAudioDownloader>();

        services.AddHttpClient<GroqTranscriptionProvider>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<TranscriptionOptions>>().Value;
            client.BaseAddress = new Uri(WithTrailingSlash(settings.Groq.BaseUrl));
            client.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TranscriptAI/1.0");
        });

        services.AddHttpClient<HuggingFaceTranscriptionProvider>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<TranscriptionOptions>>().Value;
            client.BaseAddress = new Uri(WithTrailingSlash(settings.HuggingFace.BaseUrl));
            client.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TranscriptAI/1.0");
        });

        services.TryAddTransient<ITranscriptionProvider>(sp =>
            sp.GetRequiredService<IOptions<TranscriptionOptions>>().Value.Provider switch
            {
                TranscriptionProviderKind.HuggingFace => sp.GetRequiredService<HuggingFaceTranscriptionProvider>(),
                _ => sp.GetRequiredService<GroqTranscriptionProvider>(),
            });

        services.TryAddTransient<TranscriptPipeline>();
        return services;
    }

    private static string WithTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
