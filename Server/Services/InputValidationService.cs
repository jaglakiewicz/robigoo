#region Imports

using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;

#endregion

namespace Server.Services
{
    #region Result Classes

    /// <summary>
    /// Represents the result of input validation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indicates whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors if validation failed.
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }

    /// <summary>
    /// Represents a single validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// The name of the field that failed validation.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error code for programmatic handling.
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }

    #endregion

    #region Interface

    /// <summary>
    /// Interface for input validation and sanitization service.
    /// </summary>
    /// <remarks>
    /// Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6
    /// </remarks>
    public interface IInputValidationService
    {
        /// <summary>
        /// Validates and sanitizes an input object using reflection-based validation.
        /// Checks validation attributes and SQL injection patterns.
        /// </summary>
        /// <typeparam name="T">The type of object to validate.</typeparam>
        /// <param name="input">The input object to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        ValidationResult ValidateAndSanitize<T>(T input) where T : class;

        /// <summary>
        /// Sanitizes a string by encoding HTML entities and limiting length.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <param name="maxLength">Maximum allowed length.</param>
        /// <returns>Sanitized string.</returns>
        string SanitizeString(string input, int maxLength);

        /// <summary>
        /// Validates an email address using RFC 5322 compliant patterns.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if the email is valid, false otherwise.</returns>
        bool IsValidEmail(string email);

        /// <summary>
        /// Validates a phone number against expected formats.
        /// </summary>
        /// <param name="phone">The phone number to validate.</param>
        /// <returns>True if the phone number is valid, false otherwise.</returns>
        bool IsValidPhoneNumber(string phone);

        /// <summary>
        /// Checks if the input contains SQL injection patterns.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if SQL injection patterns are detected, false otherwise.</returns>
        bool ContainsSqlInjectionPatterns(string input);

        /// <summary>
        /// Sanitizes a string for safe logging by removing/escaping newlines and control characters.
        /// This prevents log injection attacks where malicious input could forge log entries.
        /// </summary>
        /// <param name="input">The input string to sanitize for logging.</param>
        /// <param name="maxLength">Maximum allowed length (default: 500).</param>
        /// <returns>A sanitized string safe for logging.</returns>
        /// <remarks>
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// </remarks>
        string SanitizeForLogging(string? input, int maxLength = 500);

        /// <summary>
        /// Validates a date is within an acceptable range.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <param name="minDate">Minimum allowed date (optional).</param>
        /// <param name="maxDate">Maximum allowed date (optional).</param>
        /// <returns>True if the date is within the range, false otherwise.</returns>
        /// <remarks>
        /// Requirement 6.4: Server-side constraint enforcement for date ranges
        /// </remarks>
        bool IsValidDateRange(DateTime date, DateTime? minDate = null, DateTime? maxDate = null);
    }

    #endregion

    /// <summary>
    /// Service for validating and sanitizing user input.
    /// Implements defense-in-depth by validating all input against schemas,
    /// enforcing length constraints, and preventing injection attacks.
    /// </summary>
    /// <remarks>
    /// Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6
    /// </remarks>
    public class InputValidationService : IInputValidationService
    {
        #region Declarations

        private readonly ISqlInjectionDetector _sqlInjectionDetector;
        private readonly ILogger<InputValidationService> _logger;

        /// <summary>
        /// RFC 5322 compliant email regex pattern.
        /// This pattern validates email addresses according to the RFC 5322 standard.
        /// </summary>
        private static readonly Regex EmailRegex = new Regex(
            @"^(?:[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*|""(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*"")@(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?|\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-zA-Z0-9-]*[a-zA-Z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Phone number validation pattern.
        /// Supports various international formats including:
        /// - International format: +1234567890, +1 234 567 890
        /// - US format: (123) 456-7890, 123-456-7890
        /// - European format: 123 456 7890
        /// - Polish format: +48 123 456 789, 123456789
        /// </summary>
        private static readonly Regex PhoneRegex = new Regex(
            @"^(\+?\d{1,4}[\s.-]?)?(\(?\d{1,4}\)?[\s.-]?)?\d{1,4}[\s.-]?\d{1,4}[\s.-]?\d{1,9}$",
            RegexOptions.Compiled);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the InputValidationService.
        /// </summary>
        /// <param name="sqlInjectionDetector">SQL injection detection service.</param>
        /// <param name="logger">Logger instance.</param>
        public InputValidationService(
            ISqlInjectionDetector sqlInjectionDetector,
            ILogger<InputValidationService> logger)
        {
            _sqlInjectionDetector = sqlInjectionDetector;
            _logger = logger;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Validates and sanitizes an input object using reflection-based validation.
        /// </summary>
        /// <typeparam name="T">The type of object to validate.</typeparam>
        /// <param name="input">The input object to validate.</param>
        /// <returns>Validation result with any errors found.</returns>
        /// <remarks>
        /// Requirement 2.1: Validates all API request payloads against defined schemas
        /// </remarks>
        public ValidationResult ValidateAndSanitize<T>(T input) where T : class
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<ValidationError>() };

            if (input == null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = "input",
                    Message = "Input cannot be null",
                    Code = "NULL_INPUT"
                });
                return result;
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var value = property.GetValue(input);
                var propertyName = property.Name;

                // Validate Required attribute
                ValidateRequired(property, value, propertyName, result);

                // Validate string properties
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    // Check for SQL injection patterns (Requirement 1.3)
                    ValidateSqlInjection(stringValue, propertyName, result);

                    // Validate MaxLength attribute (Requirement 2.2)
                    ValidateMaxLength(property, stringValue, propertyName, result);

                    // Validate MinLength attribute
                    ValidateMinLength(property, stringValue, propertyName, result);

                    // Validate StringLength attribute
                    ValidateStringLength(property, stringValue, propertyName, result);

                    // Validate EmailAddress attribute (Requirement 2.3)
                    ValidateEmailAttribute(property, stringValue, propertyName, result);

                    // Validate Phone attribute (Requirement 2.4)
                    ValidatePhoneAttribute(property, stringValue, propertyName, result);

                    // Validate RegularExpression attribute
                    ValidateRegexAttribute(property, stringValue, propertyName, result);
                }

                // Validate numeric properties (Requirement 2.5)
                ValidateNumericRange(property, value, propertyName, result);
            }

            return result;
        }

        /// <summary>
        /// Sanitizes a string by encoding HTML entities and limiting length.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <param name="maxLength">Maximum allowed length.</param>
        /// <returns>Sanitized string.</returns>
        /// <remarks>
        /// Requirement 2.2: Enforces maximum length constraints
        /// Requirement 2.6: Strips or encodes HTML/script tags to prevent XSS
        /// </remarks>
        public string SanitizeString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Trim whitespace
            var sanitized = input.Trim();

            // Enforce maximum length (Requirement 2.2)
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
                _logger.LogDebug("Input truncated from {OriginalLength} to {MaxLength} characters",
                    input.Length, maxLength);
            }

            // Encode HTML entities to prevent XSS (Requirement 2.6)
            sanitized = HttpUtility.HtmlEncode(sanitized);

            return sanitized;
        }

        /// <summary>
        /// Validates an email address using RFC 5322 compliant patterns.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <returns>True if the email is valid, false otherwise.</returns>
        /// <remarks>
        /// Requirement 2.3: Validates email formats using RFC 5322 compliant patterns
        /// </remarks>
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Trim the email
            email = email.Trim();

            // Check length constraints (emails shouldn't be excessively long)
            if (email.Length > 254)
            {
                return false;
            }

            // Use .NET's MailAddress for primary validation (RFC 5322 compliant)
            try
            {
                var mailAddress = new MailAddress(email);
                
                // Ensure the address matches exactly (MailAddress can be lenient)
                if (mailAddress.Address != email)
                {
                    return false;
                }

                // Additional regex validation for stricter compliance
                return EmailRegex.IsMatch(email);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a phone number against expected formats.
        /// </summary>
        /// <param name="phone">The phone number to validate.</param>
        /// <returns>True if the phone number is valid, false otherwise.</returns>
        /// <remarks>
        /// Requirement 2.4: Validates phone numbers against expected formats
        /// </remarks>
        public bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            // Trim the phone number
            phone = phone.Trim();

            // Check minimum length (at least 7 digits for a valid phone number)
            var digitsOnly = Regex.Replace(phone, @"[^\d]", "");
            if (digitsOnly.Length < 7 || digitsOnly.Length > 15)
            {
                return false;
            }

            // Validate against phone regex pattern
            return PhoneRegex.IsMatch(phone);
        }

        /// <summary>
        /// Checks if the input contains SQL injection patterns.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if SQL injection patterns are detected, false otherwise.</returns>
        public bool ContainsSqlInjectionPatterns(string input)
        {
            return _sqlInjectionDetector.ContainsSqlInjectionPatterns(input);
        }

        /// <summary>
        /// Validates a date is within an acceptable range.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <param name="minDate">Minimum allowed date (optional, defaults to 1900-01-01).</param>
        /// <param name="maxDate">Maximum allowed date (optional, defaults to 100 years from now).</param>
        /// <returns>True if the date is within the range, false otherwise.</returns>
        /// <remarks>
        /// Requirement 6.4: Server-side constraint enforcement for date ranges
        /// </remarks>
        public bool IsValidDateRange(DateTime date, DateTime? minDate = null, DateTime? maxDate = null)
        {
            // Default minimum date: 1900-01-01 (reasonable minimum for most business data)
            var effectiveMinDate = minDate ?? new DateTime(1900, 1, 1);
            
            // Default maximum date: 100 years from now (reasonable maximum for future dates)
            var effectiveMaxDate = maxDate ?? DateTime.UtcNow.AddYears(100);

            // Check if date is within the valid range
            if (date < effectiveMinDate || date > effectiveMaxDate)
            {
                _logger.LogDebug(
                    "Date {Date} is outside valid range [{MinDate}, {MaxDate}]",
                    date, effectiveMinDate, effectiveMaxDate);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sanitizes a string for safe logging by removing/escaping newlines and control characters.
        /// This prevents log injection attacks where malicious input could forge log entries.
        /// </summary>
        /// <param name="input">The input string to sanitize for logging.</param>
        /// <param name="maxLength">Maximum allowed length (default: 500).</param>
        /// <returns>A sanitized string safe for logging.</returns>
        /// <remarks>
        /// Requirement 11.7: Sanitize user input before logging to prevent log injection
        /// 
        /// Log injection attacks occur when an attacker includes newline characters or other
        /// control characters in input that gets logged. This can allow them to:
        /// - Forge log entries that appear legitimate
        /// - Inject false information into audit trails
        /// - Potentially exploit log parsing tools
        /// 
        /// This method:
        /// 1. Replaces newline characters (\n, \r) with visible escape sequences
        /// 2. Removes or replaces other control characters (ASCII 0-31, 127)
        /// 3. Truncates very long strings to prevent log flooding
        /// 4. Returns a safe placeholder for null/empty input
        /// </remarks>
        public string SanitizeForLogging(string? input, int maxLength = 500)
        {
            // Handle null or empty input
            if (string.IsNullOrEmpty(input))
            {
                return "[empty]";
            }

            var sanitized = new System.Text.StringBuilder(Math.Min(input.Length, maxLength));

            foreach (char c in input)
            {
                // Check if we've reached the max length
                if (sanitized.Length >= maxLength)
                {
                    break;
                }

                // Handle specific control characters with visible escape sequences
                switch (c)
                {
                    case '\n':
                        sanitized.Append("\\n");
                        break;
                    case '\r':
                        sanitized.Append("\\r");
                        break;
                    case '\t':
                        sanitized.Append("\\t");
                        break;
                    case '\0':
                        sanitized.Append("\\0");
                        break;
                    case '\b':
                        sanitized.Append("\\b");
                        break;
                    case '\f':
                        sanitized.Append("\\f");
                        break;
                    case '\v':
                        sanitized.Append("\\v");
                        break;
                    default:
                        // Remove other control characters (ASCII 0-31 and 127)
                        // but keep printable characters
                        if (c >= 32 && c != 127)
                        {
                            sanitized.Append(c);
                        }
                        else
                        {
                            // Replace other control characters with their hex representation
                            sanitized.Append($"\\x{(int)c:X2}");
                        }
                        break;
                }
            }

            // Add truncation indicator if the input was truncated
            if (input.Length > maxLength)
            {
                sanitized.Append("...[truncated]");
            }

            return sanitized.ToString();
        }

        #endregion

        #region Methods - Private Validation Helpers

        /// <summary>
        /// Validates the Required attribute on a property.
        /// </summary>
        private void ValidateRequired(PropertyInfo property, object? value, string propertyName, ValidationResult result)
        {
            var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttr != null)
            {
                bool isInvalid = value == null ||
                                 (value is string strValue && string.IsNullOrWhiteSpace(strValue));

                if (isInvalid)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Field = propertyName,
                        Message = requiredAttr.ErrorMessage ?? "This field is required",
                        Code = "REQUIRED"
                    });
                }
            }
        }

        /// <summary>
        /// Validates string for SQL injection patterns.
        /// </summary>
        private void ValidateSqlInjection(string value, string propertyName, ValidationResult result)
        {
            if (_sqlInjectionDetector.ContainsSqlInjectionPatternsWithLogging(value, propertyName))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = "Input contains invalid characters",
                    Code = "INVALID_INPUT"
                });
            }
        }

        /// <summary>
        /// Validates the MaxLength attribute on a property.
        /// </summary>
        /// <remarks>
        /// Requirement 2.2: Enforces maximum length constraints
        /// </remarks>
        private void ValidateMaxLength(PropertyInfo property, string value, string propertyName, ValidationResult result)
        {
            var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLengthAttr != null && value.Length > maxLengthAttr.Length)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = maxLengthAttr.ErrorMessage ?? $"Maximum length is {maxLengthAttr.Length} characters",
                    Code = "MAX_LENGTH_EXCEEDED"
                });
            }
        }

        /// <summary>
        /// Validates the MinLength attribute on a property.
        /// </summary>
        private void ValidateMinLength(PropertyInfo property, string value, string propertyName, ValidationResult result)
        {
            var minLengthAttr = property.GetCustomAttribute<MinLengthAttribute>();
            if (minLengthAttr != null && value.Length < minLengthAttr.Length)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = minLengthAttr.ErrorMessage ?? $"Minimum length is {minLengthAttr.Length} characters",
                    Code = "MIN_LENGTH_EXCEEDED"
                });
            }
        }

        /// <summary>
        /// Validates the StringLength attribute on a property.
        /// </summary>
        private void ValidateStringLength(PropertyInfo property, string value, string propertyName, ValidationResult result)
        {
            var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                if (value.Length > stringLengthAttr.MaximumLength)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Field = propertyName,
                        Message = stringLengthAttr.ErrorMessage ?? $"Maximum length is {stringLengthAttr.MaximumLength} characters",
                        Code = "STRING_LENGTH_EXCEEDED"
                    });
                }

                if (stringLengthAttr.MinimumLength > 0 && value.Length < stringLengthAttr.MinimumLength)
                {
                    result.IsValid = false;
                    result.Errors.Add(new ValidationError
                    {
                        Field = propertyName,
                        Message = stringLengthAttr.ErrorMessage ?? $"Minimum length is {stringLengthAttr.MinimumLength} characters",
                        Code = "STRING_LENGTH_BELOW_MINIMUM"
                    });
                }
            }
        }

        /// <summary>
        /// Validates the EmailAddress attribute on a property.
        /// </summary>
        /// <remarks>
        /// Requirement 2.3: Validates email formats using RFC 5322 compliant patterns
        /// </remarks>
        private void ValidateEmailAttribute(PropertyInfo property, string value, string propertyName, ValidationResult result)
        {
            var emailAttr = property.GetCustomAttribute<EmailAddressAttribute>();
            if (emailAttr != null && !IsValidEmail(value))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = emailAttr.ErrorMessage ?? "Invalid email format",
                    Code = "INVALID_EMAIL"
                });
            }
        }

        /// <summary>
        /// Validates the Phone attribute on a property.
        /// </summary>
        /// <remarks>
        /// Requirement 2.4: Validates phone numbers against expected formats
        /// </remarks>
        private void ValidatePhoneAttribute(PropertyInfo property, string value, string propertyName, ValidationResult result)
        {
            var phoneAttr = property.GetCustomAttribute<PhoneAttribute>();
            if (phoneAttr != null && !IsValidPhoneNumber(value))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = phoneAttr.ErrorMessage ?? "Invalid phone number format",
                    Code = "INVALID_PHONE"
                });
            }
        }

        /// <summary>
        /// Validates the RegularExpression attribute on a property.
        /// </summary>
        private void ValidateRegexAttribute(PropertyInfo property, string value, string propertyName, ValidationResult result)
        {
            var regexAttr = property.GetCustomAttribute<RegularExpressionAttribute>();
            if (regexAttr != null && !Regex.IsMatch(value, regexAttr.Pattern))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = regexAttr.ErrorMessage ?? "Value does not match the required format",
                    Code = "REGEX_MISMATCH"
                });
            }
        }

        /// <summary>
        /// Validates numeric range constraints on a property.
        /// </summary>
        /// <remarks>
        /// Requirement 2.5: Verifies numeric inputs fall within acceptable ranges
        /// </remarks>
        private void ValidateNumericRange(PropertyInfo property, object? value, string propertyName, ValidationResult result)
        {
            var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr == null || value == null)
            {
                return;
            }

            // Handle different numeric types
            bool isOutOfRange = false;
            string rangeMessage = rangeAttr.ErrorMessage ?? $"Value must be between {rangeAttr.Minimum} and {rangeAttr.Maximum}";

            try
            {
                if (value is int intValue)
                {
                    var min = Convert.ToInt32(rangeAttr.Minimum);
                    var max = Convert.ToInt32(rangeAttr.Maximum);
                    isOutOfRange = intValue < min || intValue > max;
                }
                else if (value is long longValue)
                {
                    var min = Convert.ToInt64(rangeAttr.Minimum);
                    var max = Convert.ToInt64(rangeAttr.Maximum);
                    isOutOfRange = longValue < min || longValue > max;
                }
                else if (value is double doubleValue)
                {
                    var min = Convert.ToDouble(rangeAttr.Minimum);
                    var max = Convert.ToDouble(rangeAttr.Maximum);
                    isOutOfRange = doubleValue < min || doubleValue > max;
                }
                else if (value is decimal decimalValue)
                {
                    var min = Convert.ToDecimal(rangeAttr.Minimum);
                    var max = Convert.ToDecimal(rangeAttr.Maximum);
                    isOutOfRange = decimalValue < min || decimalValue > max;
                }
                else if (value is float floatValue)
                {
                    var min = Convert.ToSingle(rangeAttr.Minimum);
                    var max = Convert.ToSingle(rangeAttr.Maximum);
                    isOutOfRange = floatValue < min || floatValue > max;
                }
                else if (value is short shortValue)
                {
                    var min = Convert.ToInt16(rangeAttr.Minimum);
                    var max = Convert.ToInt16(rangeAttr.Maximum);
                    isOutOfRange = shortValue < min || shortValue > max;
                }
                else if (value is byte byteValue)
                {
                    var min = Convert.ToByte(rangeAttr.Minimum);
                    var max = Convert.ToByte(rangeAttr.Maximum);
                    isOutOfRange = byteValue < min || byteValue > max;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating range for property {PropertyName}", propertyName);
                // If conversion fails, we can't validate the range
                return;
            }

            if (isOutOfRange)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Field = propertyName,
                    Message = rangeMessage,
                    Code = "OUT_OF_RANGE"
                });
            }
        }

        #endregion
    }
}
