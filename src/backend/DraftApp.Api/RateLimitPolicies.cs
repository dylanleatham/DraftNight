using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DraftApp.Api;

/// <summary>
/// Per-client-IP rate limits for the unauthenticated endpoints that accept secrets.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    /// Policy for endpoints that verify a PIN (limits online guessing).
    /// </summary>
    public const string Credentials = "credentials";

    /// <summary>
    /// Policy for joining an event by code (limits join-code enumeration and lobby spam).
    /// </summary>
    public const string Join = "join";

    /// <summary>
    /// Registers the rate limit policies.
    /// </summary>
    public static IServiceCollection AddDraftAppRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Credentials, context => PerIp(context, permitsPerMinute: 10));
            options.AddPolicy(Join, context => PerIp(context, permitsPerMinute: 30));
        });
    }

    private static RateLimitPartition<string> PerIp(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
}
