/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Server.Services;

namespace Server.Middleware
{
    /// <summary>
    /// Middleware that checks if a request source is blocked due to sustained rate limit violations.
    /// </summary>
    /// <remarks>
    /// Requirement 12.6: IF sustained rate limit violations occur, THEN THE Backend SHALL temporarily block the source.
    /// This middleware runs early in the pipeline to reject requests from blocked sources before any processing.
    /// </remarks>
    public class RateLimitBlockingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitBlockingMiddleware> _logger;

        public RateLimitBlockingMiddleware(RequestDelegate next, ILogger<RateLimitBlockingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IRateLimitBlockingService blockingService)
        {
            // Get identifiers to check
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userId = context.User?.FindFirst("userId")?.Value;

            // Check if IP is blocked
            var ipIdentifier = $"ip:{ipAddress}";
            if (!string.IsNullOrEmpty(ipAddress) && await blockingService.IsBlockedAsync(ipIdentifier))
            {
                var expiration = await blockingService.GetBlockExpirationAsync(ipIdentifier);
                await HandleBlockedRequest(context, ipIdentifier, expiration);
                return;
            }

            // Check if user is blocked (if authenticated)
            if (!string.IsNullOrEmpty(userId))
            {
                var userIdentifier = $"user:{userId}";
                if (await blockingService.IsBlockedAsync(userIdentifier))
                {
                    var expiration = await blockingService.GetBlockExpirationAsync(userIdentifier);
                    await HandleBlockedRequest(context, userIdentifier, expiration);
                    return;
                }
            }

            await _next(context);
        }

        private async Task HandleBlockedRequest(HttpContext context, string identifier, DateTime? expiration)
        {
            _logger.LogWarning(
                "Blocked request from {Identifier}. Block expires at {Expiration}",
                identifier,
                expiration);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            // Set Retry-After header to indicate when the block expires
            if (expiration.HasValue)
            {
                var retryAfterSeconds = (int)Math.Ceiling((expiration.Value - DateTime.UtcNow).TotalSeconds);
                if (retryAfterSeconds > 0)
                {
                    context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                }
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. You have been temporarily blocked due to sustained rate limit violations.",
                retryAfter = expiration?.ToString("O"),
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Extension methods for adding the rate limit blocking middleware.
    /// </summary>
    public static class RateLimitBlockingMiddlewareExtensions
    {
        /// <summary>
        /// Adds the rate limit blocking middleware to the application pipeline.
        /// </summary>
        /// <remarks>
        /// This middleware should be added after authentication but before rate limiting
        /// to ensure blocked users/IPs are rejected early.
        /// </remarks>
        public static IApplicationBuilder UseRateLimitBlocking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitBlockingMiddleware>();
        }
    }
}
