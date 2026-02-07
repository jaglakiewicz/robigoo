/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Microsoft.AspNetCore.Diagnostics;
using Server.Exceptions;
using Server.Middleware;
using System.Text.Json;

#endregion

namespace Server.Services
{
    /// <summary>
    /// Global exception handler that implements IExceptionHandler.
    /// Logs full details server-side and returns safe error responses to clients.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        #region Declarations

        private readonly ILogger<GlobalExceptionHandler> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        #endregion

        #region Constructor

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Handles exceptions and returns appropriate HTTP responses.
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Generate or retrieve correlation ID
            var correlationId = GetCorrelationId(httpContext, exception);

            // Log full details server-side (sanitized to prevent log injection)
            LogException(exception, correlationId, httpContext);

            // Determine response based on exception type
            var (statusCode, errorMessage, errorCode, details) = MapExceptionToResponse(exception);

            // Set response status code
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            // Add Retry-After header for rate limit and service unavailable exceptions
            AddRetryAfterHeader(httpContext, exception);

            // Build safe error response (no internal details exposed)
            var errorResponse = BuildErrorResponse(errorMessage, errorCode, correlationId, details);

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(errorResponse, JsonOptions),
                cancellationToken);

            return true;
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Gets the correlation ID from the exception, HttpContext, or generates a new one.
        /// Priority: RobigooException > HttpContext (from middleware) > Request Header > New GUID
        /// </summary>
        private static string GetCorrelationId(HttpContext httpContext, Exception exception)
        {
            // Try to get from RobigooException
            if (exception is RobigooException robigooEx)
            {
                return robigooEx.CorrelationId;
            }

            // Try to get from HttpContext.Items (set by CorrelationIdMiddleware)
            var contextCorrelationId = httpContext.GetCorrelationId();
            if (!string.IsNullOrWhiteSpace(contextCorrelationId))
            {
                return contextCorrelationId;
            }

            // Try to get from request headers (fallback)
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var headerValue) &&
                !string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.ToString();
            }

            // Generate new correlation ID
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Logs the exception with full details server-side.
        /// Sanitizes user input to prevent log injection.
        /// </summary>
        private void LogException(Exception exception, string correlationId, HttpContext httpContext)
        {
            var path = SanitizeForLogging(httpContext.Request.Path.ToString());
            var method = httpContext.Request.Method;
            var ipAddress = GetClientIpAddress(httpContext);

            if (exception is RobigooException robigooEx)
            {
                // Log application exceptions at Warning level (expected errors)
                _logger.LogWarning(
                    exception,
                    "Application exception occurred. CorrelationId: {CorrelationId}, Code: {ErrorCode}, Path: {Path}, Method: {Method}, IP: {IpAddress}",
                    correlationId,
                    robigooEx.Code,
                    path,
                    method,
                    ipAddress);
            }
            else
            {
                // Log unexpected exceptions at Error level
                _logger.LogError(
                    exception,
                    "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}, IP: {IpAddress}",
                    correlationId,
                    path,
                    method,
                    ipAddress);
            }

            // Log security-related exceptions with elevated severity
            if (exception is Exceptions.SecurityException secEx)
            {
                _logger.LogWarning(
                    "Security event detected. CorrelationId: {CorrelationId}, EventType: {EventType}, Path: {Path}, IP: {IpAddress}",
                    correlationId,
                    secEx.EventType ?? "Unknown",
                    path,
                    ipAddress);
            }
        }

        /// <summary>
        /// Maps exception types to HTTP status codes and safe error messages.
        /// </summary>
        private static (int StatusCode, string Message, string Code, object? Details) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                Exceptions.ValidationException validationEx => (
                    StatusCodes.Status400BadRequest,
                    validationEx.Message,
                    validationEx.Code,
                    validationEx.Errors.Count > 0 ? validationEx.Errors.Select(e => new { e.Field, e.Message, e.Code }) : null
                ),

                Exceptions.AuthenticationException authEx => (
                    StatusCodes.Status401Unauthorized,
                    "Authentication required",
                    authEx.Code,
                    null
                ),

                Exceptions.AuthorizationException authzEx => (
                    StatusCodes.Status403Forbidden,
                    "Access denied",
                    authzEx.Code,
                    null
                ),

                Exceptions.NotFoundException notFoundEx => (
                    StatusCodes.Status404NotFound,
                    "Resource not found",
                    notFoundEx.Code,
                    null
                ),

                Exceptions.ConcurrencyException concurrencyEx => (
                    StatusCodes.Status409Conflict,
                    "The resource was modified by another user. Please refresh and try again.",
                    concurrencyEx.Code,
                    null
                ),

                Exceptions.RateLimitException rateLimitEx => (
                    StatusCodes.Status429TooManyRequests,
                    "Too many requests. Please try again later.",
                    rateLimitEx.Code,
                    null
                ),

                Exceptions.ServiceUnavailableException serviceEx => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Service temporarily unavailable. Please try again later.",
                    serviceEx.Code,
                    null
                ),

                Exceptions.SecurityException securityEx => (
                    StatusCodes.Status400BadRequest,
                    "Invalid request",
                    securityEx.Code,
                    null
                ),

                // For any other exception, return generic 500 error
                // IMPORTANT: Never expose internal details to clients
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred",
                    "INTERNAL_ERROR",
                    null
                )
            };
        }

        /// <summary>
        /// Adds Retry-After header for rate limit and service unavailable exceptions.
        /// </summary>
        private static void AddRetryAfterHeader(HttpContext httpContext, Exception exception)
        {
            TimeSpan? retryAfter = exception switch
            {
                Exceptions.RateLimitException rateLimitEx => rateLimitEx.RetryAfter,
                Exceptions.ServiceUnavailableException serviceEx => serviceEx.RetryAfter,
                _ => null
            };

            if (retryAfter.HasValue)
            {
                httpContext.Response.Headers.Append("Retry-After", ((int)retryAfter.Value.TotalSeconds).ToString());
            }
        }

        /// <summary>
        /// Builds a safe error response object.
        /// </summary>
        private static object BuildErrorResponse(string message, string code, string correlationId, object? details)
        {
            var response = new Dictionary<string, object>
            {
                ["error"] = message,
                ["code"] = code,
                ["correlationId"] = correlationId,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            if (details != null)
            {
                response["details"] = details;
            }

            return response;
        }

        /// <summary>
        /// Sanitizes input for logging to prevent log injection attacks.
        /// </summary>
        private static string SanitizeForLogging(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Remove newlines and control characters to prevent log injection
            return input
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", " ");
        }

        /// <summary>
        /// Gets the client IP address from the request.
        /// </summary>
        private static string? GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return SanitizeForLogging(forwardedFor.Split(',').FirstOrDefault()?.Trim());
            }
            return context.Connection.RemoteIpAddress?.ToString();
        }

        #endregion
    }
}
