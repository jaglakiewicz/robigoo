namespace Server.Exceptions
{
    /// <summary>
    /// Base exception class for all SKO application exceptions.
    /// Provides a consistent error code and correlation ID for tracing.
    /// </summary>
    public abstract class SkoException : Exception
    {
        /// <summary>
        /// A machine-readable error code for categorizing the exception.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// A unique identifier for tracing this exception across logs and responses.
        /// </summary>
        public string CorrelationId { get; }

        protected SkoException(string message, string code, string? correlationId = null)
            : base(message)
        {
            Code = code;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }

        protected SkoException(string message, string code, Exception innerException, string? correlationId = null)
            : base(message, innerException)
        {
            Code = code;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Exception thrown when input validation fails.
    /// Maps to HTTP 400 Bad Request.
    /// </summary>
    public class ValidationException : SkoException
    {
        public IReadOnlyList<ValidationError> Errors { get; }

        public ValidationException(string message, IEnumerable<ValidationError>? errors = null, string? correlationId = null)
            : base(message, "VALIDATION_ERROR", correlationId)
        {
            Errors = errors?.ToList().AsReadOnly() ?? new List<ValidationError>().AsReadOnly();
        }

        public ValidationException(string message, string field, string? correlationId = null)
            : base(message, "VALIDATION_ERROR", correlationId)
        {
            Errors = new List<ValidationError> { new ValidationError(field, message) }.AsReadOnly();
        }
    }

    /// <summary>
    /// Represents a single validation error for a specific field.
    /// </summary>
    public class ValidationError
    {
        public string Field { get; }
        public string Message { get; }
        public string? Code { get; }

        public ValidationError(string field, string message, string? code = null)
        {
            Field = field;
            Message = message;
            Code = code;
        }
    }

    /// <summary>
    /// Exception thrown when authentication fails.
    /// Maps to HTTP 401 Unauthorized.
    /// </summary>
    public class AuthenticationException : SkoException
    {
        public AuthenticationException(string message = "Authentication required", string? correlationId = null)
            : base(message, "AUTHENTICATION_ERROR", correlationId)
        {
        }
    }

    /// <summary>
    /// Exception thrown when authorization fails.
    /// Maps to HTTP 403 Forbidden.
    /// </summary>
    public class AuthorizationException : SkoException
    {
        public string? Resource { get; }
        public string? Action { get; }

        public AuthorizationException(string message = "Access denied", string? resource = null, string? action = null, string? correlationId = null)
            : base(message, "AUTHORIZATION_ERROR", correlationId)
        {
            Resource = resource;
            Action = action;
        }
    }

    /// <summary>
    /// Exception thrown when a database concurrency conflict occurs.
    /// Maps to HTTP 409 Conflict.
    /// </summary>
    public class ConcurrencyException : SkoException
    {
        public string? EntityType { get; }
        public string? EntityId { get; }

        public ConcurrencyException(string message = "The resource was modified by another user", string? entityType = null, string? entityId = null, string? correlationId = null)
            : base(message, "CONCURRENCY_ERROR", correlationId)
        {
            EntityType = entityType;
            EntityId = entityId;
        }

        public ConcurrencyException(string message, Exception innerException, string? correlationId = null)
            : base(message, "CONCURRENCY_ERROR", innerException, correlationId)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a requested resource is not found.
    /// Maps to HTTP 404 Not Found.
    /// </summary>
    public class NotFoundException : SkoException
    {
        public string? ResourceType { get; }
        public string? ResourceId { get; }

        public NotFoundException(string message = "Resource not found", string? resourceType = null, string? resourceId = null, string? correlationId = null)
            : base(message, "NOT_FOUND", correlationId)
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
    }

    /// <summary>
    /// Exception thrown when rate limits are exceeded.
    /// Maps to HTTP 429 Too Many Requests.
    /// </summary>
    public class RateLimitException : SkoException
    {
        public TimeSpan? RetryAfter { get; }

        public RateLimitException(string message = "Rate limit exceeded", TimeSpan? retryAfter = null, string? correlationId = null)
            : base(message, "RATE_LIMIT_EXCEEDED", correlationId)
        {
            RetryAfter = retryAfter;
        }
    }

    /// <summary>
    /// Exception thrown when the service is temporarily unavailable.
    /// Maps to HTTP 503 Service Unavailable.
    /// </summary>
    public class ServiceUnavailableException : SkoException
    {
        public TimeSpan? RetryAfter { get; }

        public ServiceUnavailableException(string message = "Service temporarily unavailable", TimeSpan? retryAfter = null, string? correlationId = null)
            : base(message, "SERVICE_UNAVAILABLE", correlationId)
        {
            RetryAfter = retryAfter;
        }
    }

    /// <summary>
    /// Exception thrown when a security-related issue is detected.
    /// Maps to HTTP 400 Bad Request (to avoid information leakage).
    /// </summary>
    public class SecurityException : SkoException
    {
        public string? EventType { get; }

        public SecurityException(string message = "Security violation detected", string? eventType = null, string? correlationId = null)
            : base(message, "SECURITY_ERROR", correlationId)
        {
            EventType = eventType;
        }
    }
}
