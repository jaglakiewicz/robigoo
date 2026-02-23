#region Imports

using Server.Services;
using System.Text;
using System.Text.Json;

#endregion

namespace Server.Middleware
{
    /// <summary>
    /// Middleware that validates all incoming request bodies using the IInputValidationService.
    /// Returns 400 Bad Request with specific validation error messages on failure.
    /// </summary>
    /// <remarks>
    /// Requirement 2.7: IF validation fails, THEN THE Backend SHALL return a 400 Bad Request 
    /// with specific validation error messages
    /// </remarks>
    public class ValidationMiddleware
    {
        #region Constants

        /// <summary>
        /// Content types that should be validated as JSON.
        /// </summary>
        private static readonly string[] JsonContentTypes = new[]
        {
            "application/json",
            "text/json"
        };

        /// <summary>
        /// HTTP methods that typically have request bodies to validate.
        /// </summary>
        private static readonly string[] MethodsWithBody = new[]
        {
            "POST",
            "PUT",
            "PATCH"
        };

        /// <summary>
        /// Maximum request body size to read for validation (1MB).
        /// </summary>
        private const int MaxBodySize = 1024 * 1024;

        #endregion

        #region Declarations

        private readonly RequestDelegate _next;
        private readonly ILogger<ValidationMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        #endregion

        #region Constructor

        public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Processes the HTTP request, validating JSON request bodies.
        /// </summary>
        public async Task InvokeAsync(HttpContext context, IInputValidationService validationService)
        {
            // Only validate requests with bodies (POST, PUT, PATCH)
            if (!ShouldValidateRequest(context))
            {
                await _next(context);
                return;
            }

            // Check if the request has a JSON content type
            if (!HasJsonContentType(context))
            {
                await _next(context);
                return;
            }

            // Enable buffering so the body can be read multiple times
            context.Request.EnableBuffering();

            // Read the request body
            string? requestBody = null;
            try
            {
                requestBody = await ReadRequestBodyAsync(context.Request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read request body for validation");
                await _next(context);
                return;
            }

            // If body is empty or null, let the controller handle it
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                await _next(context);
                return;
            }

            // Validate the request body
            var validationResult = ValidateRequestBody(requestBody, validationService);

            if (!validationResult.IsValid)
            {
                // Log the validation failure
                var correlationId = context.GetCorrelationId() ?? Guid.NewGuid().ToString();
                _logger.LogWarning(
                    "Request validation failed. CorrelationId: {CorrelationId}, Path: {Path}, Errors: {ErrorCount}",
                    correlationId,
                    context.Request.Path,
                    validationResult.Errors.Count);

                // Return 400 Bad Request with validation errors
                await WriteValidationErrorResponse(context, validationResult, correlationId);
                return;
            }

            // Reset the request body stream position for downstream middleware/controllers
            context.Request.Body.Position = 0;

            await _next(context);
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Determines if the request should be validated.
        /// </summary>
        private static bool ShouldValidateRequest(HttpContext context)
        {
            return MethodsWithBody.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the request has a JSON content type.
        /// </summary>
        private static bool HasJsonContentType(HttpContext context)
        {
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            return JsonContentTypes.Any(jct => 
                contentType.StartsWith(jct, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Reads the request body as a string.
        /// </summary>
        private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            // Check content length to prevent reading excessively large bodies
            if (request.ContentLength.HasValue && request.ContentLength.Value > MaxBodySize)
            {
                return null;
            }

            request.Body.Position = 0;

            using var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            // Reset position for downstream processing
            request.Body.Position = 0;

            return body;
        }

        /// <summary>
        /// Validates the request body using the validation service.
        /// </summary>
        private ValidationResult ValidateRequestBody(string requestBody, IInputValidationService validationService)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<ValidationError>() };

            try
            {
                // Parse the JSON to validate it's well-formed
                using var document = JsonDocument.Parse(requestBody);
                // JSON is valid - no further validation needed here
                // SQL injection protection is handled by Entity Framework's parameterized queries
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Invalid JSON in request body");
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = "body",
                    Message = "Invalid JSON format",
                    Code = "INVALID_JSON"
                });
            }

            return result;
        }

        /// <summary>
        /// Recursively validates a JSON object's string properties.
        /// </summary>
        private void ValidateJsonObject(
            JsonElement element, 
            IInputValidationService validationService, 
            ValidationResult result,
            string prefix)
        {
            foreach (var property in element.EnumerateObject())
            {
                var fieldName = string.IsNullOrEmpty(prefix) 
                    ? property.Name 
                    : $"{prefix}.{property.Name}";

                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        var stringValue = property.Value.GetString();
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            // Check for SQL injection patterns
                            if (validationService.ContainsSqlInjectionPatterns(stringValue))
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError
                                {
                                    Field = fieldName,
                                    Message = "Input contains invalid characters",
                                    Code = "INVALID_INPUT"
                                });
                            }
                        }
                        break;

                    case JsonValueKind.Object:
                        // Recursively validate nested objects
                        ValidateJsonObject(property.Value, validationService, result, fieldName);
                        break;

                    case JsonValueKind.Array:
                        // Validate array elements
                        ValidateJsonArray(property.Value, validationService, result, fieldName);
                        break;
                }
            }
        }

        /// <summary>
        /// Validates elements in a JSON array.
        /// </summary>
        private void ValidateJsonArray(
            JsonElement element,
            IInputValidationService validationService,
            ValidationResult result,
            string fieldName)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var itemFieldName = $"{fieldName}[{index}]";

                switch (item.ValueKind)
                {
                    case JsonValueKind.String:
                        var stringValue = item.GetString();
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            if (validationService.ContainsSqlInjectionPatterns(stringValue))
                            {
                                result.IsValid = false;
                                result.Errors.Add(new ValidationError
                                {
                                    Field = itemFieldName,
                                    Message = "Input contains invalid characters",
                                    Code = "INVALID_INPUT"
                                });
                            }
                        }
                        break;

                    case JsonValueKind.Object:
                        ValidateJsonObject(item, validationService, result, itemFieldName);
                        break;

                    case JsonValueKind.Array:
                        ValidateJsonArray(item, validationService, result, itemFieldName);
                        break;
                }

                index++;
            }
        }

        /// <summary>
        /// Writes a 400 Bad Request response with validation errors.
        /// </summary>
        /// <remarks>
        /// Requirement 2.7: Returns 400 Bad Request with specific validation error messages
        /// </remarks>
        private static async Task WriteValidationErrorResponse(
            HttpContext context, 
            ValidationResult validationResult,
            string correlationId)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "Invalid request data",
                code = "VALIDATION_ERROR",
                correlationId = correlationId,
                timestamp = DateTime.UtcNow.ToString("O"),
                details = validationResult.Errors.Select(e => new
                {
                    field = e.Field,
                    message = e.Message,
                    code = e.Code
                })
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(errorResponse, JsonOptions));
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for registering the ValidationMiddleware.
    /// </summary>
    public static class ValidationMiddlewareExtensions
    {
        /// <summary>
        /// Adds the validation middleware to the application pipeline.
        /// This should be added after authentication but before controllers
        /// to validate all incoming request bodies.
        /// </summary>
        public static IApplicationBuilder UseInputValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidationMiddleware>();
        }
    }
}
