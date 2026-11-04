using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Endpoints that run Argon2id (about 19 MiB and 45 ms of CPU per call). At most a few run at once and the rest
    /// queue, so a burst of logins can't exhaust memory or the thread pool; only a burst beyond the queue gets 429.
    /// Normal use, including automated contract tests logging in from one IP, never waits noticeably.
    /// (A per-IP rate limit is deliberately left for deployment: behind the host's proxy every client shares the
    /// proxy's IP until forwarded headers are configured.)
    /// </summary>
    public const string PasswordHashingPolicy = "password-hashing";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddConcurrencyLimiter(PasswordHashingPolicy, limiter =>
            {
                limiter.PermitLimit = 4;
                limiter.QueueLimit = 100;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });
}
