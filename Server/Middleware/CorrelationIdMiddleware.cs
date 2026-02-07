/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

namespace Server.Middleware
{
    /// <summary>
    /// Middleware that generates and attaches correlation IDs to requests for distributed tracing.
    /// The correlation ID is:
    /// 1. Generated for each incoming request if not already present in headers
    /// 2. Attached to the HttpContext.Items for use throughout the request pipeline
    /// 3. Included in the response headers (X-Correlation-ID)
    /// 4. Available for structured logging via ILogger scopes
    /// </summary>
    public class CorrelationIdMiddleware
    {
        #region Constants

        /// <summary>
        /// The header name used for correlation ID in both request and response.
        /// </summary>
        public const string CorrelationIdHeaderName = "X-Correlation-ID";

        /// <summary>
        /// The key used to store the correlation ID in HttpContext.Items.
        /// </summary>
        public const string CorrelationIdItemKey = "CorrelationId";

        #endregion

        #region Declarations

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        #endregion

        #region Constructor

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Processes the HTTP request, generating or extracting a correlation ID
        /// and making it available throughout the request pipeline.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Get or generate correlation ID
            var correlationId = GetOrGenerateCorrelationId(context);

            // Store in HttpContext.Items for access throughout the request pipeline
            context.Items[CorrelationIdItemKey] = correlationId;

            // Add correlation ID to response headers (before calling next to ensure it's set even on errors)
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
                }
                return Task.CompletedTask;
            });

            // Create a logging scope with the correlation ID for structured logging
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                _logger.LogDebug("Request started with CorrelationId: {CorrelationId}", correlationId);

                await _next(context);

                _logger.LogDebug("Request completed with CorrelationId: {CorrelationId}", correlationId);
            }
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Gets the correlation ID from the request header or generates a new one.
        /// </summary>
        private static string GetOrGenerateCorrelationId(HttpContext context)
        {
            // Try to get from request headers
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue) &&
                !string.IsNullOrWhiteSpace(headerValue))
            {
                var correlationId = headerValue.ToString().Trim();
                
                // Validate the correlation ID format (should be a valid GUID or reasonable string)
                // Limit length to prevent abuse
                if (correlationId.Length <= 100 && IsValidCorrelationId(correlationId))
                {
                    return correlationId;
                }
            }

            // Generate new correlation ID
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Validates that the correlation ID is safe to use (no injection attacks).
        /// </summary>
        private static bool IsValidCorrelationId(string correlationId)
        {
            // Allow alphanumeric characters, hyphens, and underscores
            // This prevents log injection and other attacks
            foreach (var c in correlationId)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for registering the CorrelationIdMiddleware.
    /// </summary>
    public static class CorrelationIdMiddlewareExtensions
    {
        /// <summary>
        /// Adds the correlation ID middleware to the application pipeline.
        /// This should be added early in the pipeline to ensure correlation IDs
        /// are available for all subsequent middleware and handlers.
        /// </summary>
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }

        /// <summary>
        /// Gets the correlation ID from the HttpContext.
        /// Returns null if no correlation ID has been set.
        /// </summary>
        public static string? GetCorrelationId(this HttpContext context)
        {
            if (context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var correlationId))
            {
                return correlationId as string;
            }
            return null;
        }
    }
}
