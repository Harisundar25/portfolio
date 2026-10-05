using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TranscriptAI.Api;

/// <summary>Marks endpoints that spend the demo's AI quota (the <c>/api/transcriptions</c> group).</summary>
internal sealed class DemoQuotaMetadata
{
    public static readonly DemoQuotaMetadata Instance = new();

    private DemoQuotaMetadata()
    {
    }
}

/// <summary>
/// Guard rails for the public demo, which spends a free Groq quota:
/// <list type="bullet">
/// <item>requests per visitor per window, then concurrent jobs — enforced by the rate-limiting middleware;</item>
/// <item>jobs per day across all visitors — <see cref="DailyQuota"/>, spent only when a transcription actually starts.</item>
/// </list>
/// The daily cap is deliberately not a third chained limiter: the middleware tries <c>AttemptAcquire</c> and then
/// <c>AcquireAsync</c>, and fixed-window permits taken by an earlier limiter are not returned when a later one rejects.
/// </summary>
internal static class DemoRateLimiting
{
    public static IServiceCollection AddDemoRateLimiting(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<DailyQuota>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;

            // Per visitor first, so visitors over their limit are turned away without taking a place in the job queue.
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                ForQuotaEndpoints(static (context, demo) => RateLimitPartition.GetFixedWindowLimiter(
                    "client:" + ClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = demo.PermitLimitPerWindow,
                        Window = TimeSpan.FromMinutes(demo.WindowMinutes),
                    })),
                ForQuotaEndpoints(static (_, demo) => RateLimitPartition.GetConcurrencyLimiter(
                    "jobs",
                    _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = demo.MaxConcurrentJobs,
                        QueueLimit = demo.MaxConcurrentJobs,
                    })));
        });

        return services;
    }

    private static PartitionedRateLimiter<HttpContext> ForQuotaEndpoints(
        Func<HttpContext, DemoOptions, RateLimitPartition<string>> partition) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
            context.GetEndpoint()?.Metadata.GetMetadata<DemoQuotaMetadata>() is null
                ? RateLimitPartition.GetNoLimiter(string.Empty)
                : partition(context, context.RequestServices.GetRequiredService<IOptions<DemoOptions>>().Value));

    /// <summary>The visitor's IP (already resolved from X-Forwarded-For behind App Service).</summary>
    internal static string ClientKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return "unknown";
        }

        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    internal static void SetRetryAfter(HttpResponse response, TimeSpan wait) =>
        response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;

        string detail;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            SetRetryAfter(http.Response, retryAfter);
            detail = $"You've sent several requests in a short time. Please try again in {Describe(retryAfter)}.";
        }
        else
        {
            // The concurrency limiter rejects without a Retry-After hint.
            detail = "The demo is busy with other transcriptions right now. Please try again in a minute.";
        }

        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = detail,
            },
        });
    }

    private static string Describe(TimeSpan wait)
    {
        var minutes = (int)Math.Ceiling(wait.TotalMinutes);
        return minutes <= 1 ? "a minute" : $"{minutes} minutes";
    }
}

/// <summary>
/// Caps how many transcriptions the demo starts per UTC day across all visitors. Kept in memory,
/// so the count resets at midnight UTC and whenever the app restarts.
/// </summary>
internal sealed class DailyQuota(IOptions<DemoOptions> options, TimeProvider time)
{
    private readonly Lock _gate = new();
    private DateOnly _day;
    private int _used;

    /// <summary>Spends one unit of today's allowance; when none is left, reports how long until it resets.</summary>
    public bool TryConsume(out TimeSpan retryAfter)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        lock (_gate)
        {
            if (today != _day)
            {
                _day = today;
                _used = 0;
            }

            if (_used < options.Value.DailyLimit)
            {
                _used++;
                retryAfter = TimeSpan.Zero;
                return true;
            }
        }

        retryAfter = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) - now;
        return false;
    }
}
