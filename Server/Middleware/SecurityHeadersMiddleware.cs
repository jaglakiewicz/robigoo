/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

namespace Server.Middleware
{
    /// <summary>
    /// Middleware that adds security headers to all HTTP responses.
    /// Implements requirements 10.1, 10.2, 10.3, 10.4, 10.7 for API security headers.
    /// 
    /// Headers added:
    /// - Content-Security-Policy: Prevents XSS and injection attacks
    /// - X-Content-Type-Options: nosniff - Prevents MIME type sniffing
    /// - X-Frame-Options: DENY - Prevents clickjacking
    /// - X-XSS-Protection: 1; mode=block - Legacy XSS protection for older browsers
    /// - Referrer-Policy: strict-origin-when-cross-origin - Controls referrer information
    /// - Permissions-Policy: Restricts browser features
    /// - Cache-Control: no-store, no-cache, must-revalidate - For sensitive endpoints
    /// - Strict-Transport-Security: max-age=31536000; includeSubDomains - HSTS for production
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        #region Constants

        /// <summary>
        /// Content Security Policy header value.
        /// Restricts sources for scripts, styles, images, fonts, and connections.
        /// </summary>
        private const string ContentSecurityPolicy = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'";

        /// <summary>
        /// Permissions Policy header value.
        /// Disables geolocation, microphone, and camera access.
        /// </summary>
        private const string PermissionsPolicy = "geolocation=(), microphone=(), camera=()";

        /// <summary>
        /// Cache-Control header value for sensitive endpoints.
        /// Prevents caching of sensitive data.
        /// Requirement 10.7: THE Backend SHALL include Cache-Control headers to prevent caching of sensitive data
        /// </summary>
        private const string CacheControlSensitive = "no-store, no-cache, must-revalidate, private";

        /// <summary>
        /// Strict-Transport-Security header value for production.
        /// max-age=31536000 (1 year), includeSubDomains
        /// Requirement 10.4: THE Backend SHALL include Strict-Transport-Security header in production
        /// </summary>
        private const string StrictTransportSecurity = "max-age=31536000; includeSubDomains";

        /// <summary>
        /// Sensitive endpoint path prefixes that should have Cache-Control: no-store headers.
        /// </summary>
        private static readonly string[] SensitiveEndpointPrefixes = new[]
        {
            "/api/auth",
            "/api/users",
            "/api/protocols",
            "/api/inspection"
        };

        #endregion

        #region Declarations

        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        #endregion

        #region Constructor

        public SecurityHeadersMiddleware(
            RequestDelegate next, 
            IWebHostEnvironment environment,
            ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Processes the HTTP request and adds security headers to the response.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers before calling next to ensure they're set even on errors
            context.Response.OnStarting(() =>
            {
                AddSecurityHeaders(context);
                return Task.CompletedTask;
            });

            await _next(context);
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Adds all security headers to the response.
        /// </summary>
        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Requirement 10.1: Content-Security-Policy header
            // Prevents XSS and injection attacks by restricting content sources
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers.Append("Content-Security-Policy", ContentSecurityPolicy);
            }

            // Requirement 10.2: X-Content-Type-Options: nosniff
            // Prevents MIME type sniffing attacks
            if (!headers.ContainsKey("X-Content-Type-Options"))
            {
                headers.Append("X-Content-Type-Options", "nosniff");
            }

            // Requirement 10.3: X-Frame-Options: DENY
            // Prevents clickjacking attacks by disallowing framing
            if (!headers.ContainsKey("X-Frame-Options"))
            {
                headers.Append("X-Frame-Options", "DENY");
            }

            // X-XSS-Protection: Legacy XSS protection for older browsers
            // Note: Modern browsers have deprecated this, but it provides defense-in-depth
            if (!headers.ContainsKey("X-XSS-Protection"))
            {
                headers.Append("X-XSS-Protection", "1; mode=block");
            }

            // Referrer-Policy: Controls how much referrer information is sent
            if (!headers.ContainsKey("Referrer-Policy"))
            {
                headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            // Permissions-Policy: Restricts browser features
            if (!headers.ContainsKey("Permissions-Policy"))
            {
                headers.Append("Permissions-Policy", PermissionsPolicy);
            }

            // Requirement 10.7: Cache-Control headers for sensitive endpoints
            // Prevents caching of sensitive data like authentication tokens and user data
            if (IsSensitiveEndpoint(context.Request.Path))
            {
                if (!headers.ContainsKey("Cache-Control"))
                {
                    headers.Append("Cache-Control", CacheControlSensitive);
                }
                
                // Also add Pragma for HTTP/1.0 compatibility
                if (!headers.ContainsKey("Pragma"))
                {
                    headers.Append("Pragma", "no-cache");
                }
                
                // Expires header for additional cache prevention
                if (!headers.ContainsKey("Expires"))
                {
                    headers.Append("Expires", "0");
                }
            }

            // Requirement 10.4: Strict-Transport-Security for production
            // Forces HTTPS connections for the specified duration
            // Only add in production to avoid issues with local development
            if (!_environment.IsDevelopment())
            {
                if (!headers.ContainsKey("Strict-Transport-Security"))
                {
                    headers.Append("Strict-Transport-Security", StrictTransportSecurity);
                }
            }
        }

        /// <summary>
        /// Determines if the request path is a sensitive endpoint that requires
        /// Cache-Control: no-store headers.
        /// </summary>
        private static bool IsSensitiveEndpoint(PathString path)
        {
            if (!path.HasValue)
            {
                return false;
            }

            var pathValue = path.Value;
            
            foreach (var prefix in SensitiveEndpointPrefixes)
            {
                if (pathValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for registering the SecurityHeadersMiddleware.
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        /// <summary>
        /// Adds the security headers middleware to the application pipeline.
        /// This should be added early in the pipeline to ensure security headers
        /// are present on all responses, including error responses.
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
