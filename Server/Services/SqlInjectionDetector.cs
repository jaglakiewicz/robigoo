#region Imports

using Microsoft.Extensions.Logging;

#endregion

namespace Server.Services
{
    #region Interfaces

    /// <summary>
    /// Interface for SQL injection detection service.
    /// </summary>
    public interface ISqlInjectionDetector
    {
        /// <summary>
        /// Checks if the input string contains SQL injection patterns.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if SQL injection patterns are detected, false otherwise.</returns>
        bool ContainsSqlInjectionPatterns(string? input);

        /// <summary>
        /// Checks if the input string contains SQL injection patterns and logs the attempt if detected.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <param name="fieldName">The name of the field being validated (for logging).</param>
        /// <param name="ipAddress">The IP address of the request (for logging).</param>
        /// <returns>True if SQL injection patterns are detected, false otherwise.</returns>
        bool ContainsSqlInjectionPatternsWithLogging(string? input, string? fieldName = null, string? ipAddress = null);
    }

    #endregion

    /// <summary>
    /// Service for detecting SQL injection patterns in user input.
    /// Implements pattern matching against known SQL injection attack vectors.
    /// </summary>
    /// <remarks>
    /// This service provides defense-in-depth by detecting SQL injection patterns
    /// before they reach the database layer. It should be used in conjunction with
    /// parameterized queries, not as a replacement.
    /// 
    /// Requirements: 1.3, 1.6
    /// </remarks>
    public class SqlInjectionDetector : ISqlInjectionDetector
    {
        #region Declarations

        private readonly ILogger<SqlInjectionDetector> _logger;

        /// <summary>
        /// Comprehensive list of SQL injection patterns to detect.
        /// Includes SQL keywords, comment sequences, and common attack patterns.
        /// </summary>
        private static readonly string[] DangerousPatterns = new[]
        {
            // SQL comment sequences
            "--",
            ";--",
            "/*",
            "*/",
            
            // SQL variable prefixes
            "@@",
            
            // SQL string/type functions commonly used in injection
            "char(",
            "nchar(",
            "varchar(",
            "nvarchar(",
            "ascii(",
            "unicode(",
            "concat(",
            
            // SQL DDL/DML keywords
            "alter",
            "begin",
            "cast",
            "create",
            "cursor",
            "declare",
            "delete",
            "drop",
            "end",
            "exec",
            "execute",
            "fetch",
            "insert",
            "kill",
            "select",
            "update",
            "truncate",
            "merge",
            
            // SQL system objects
            "sys",
            "sysobjects",
            "syscolumns",
            "systables",
            "information_schema",
            "table",
            
            // SQL logical operators commonly used in injection
            "union",
            "having",
            "group by",
            "order by",
            
            // SQL transaction keywords
            "commit",
            "rollback",
            "savepoint",
            
            // SQL administrative commands
            "shutdown",
            "waitfor",
            "delay",
            
            // Common injection patterns
            "or 1=1",
            "or '1'='1",
            "or \"1\"=\"1",
            "' or '",
            "\" or \"",
            "1=1",
            "1' or",
            "1\" or",
            
            // Hex encoding patterns
            "0x",
            
            // SQL Server specific
            "xp_",
            "sp_",
            
            // SQLite specific
            "sqlite_",
            "pragma"
        };

        /// <summary>
        /// Additional patterns that require word boundary checking to avoid false positives.
        /// These are common words that could appear in legitimate input but are dangerous in SQL context.
        /// </summary>
        private static readonly string[] WordBoundaryPatterns = new[]
        {
            "table",
            "select",
            "insert",
            "update",
            "delete",
            "drop",
            "create",
            "alter",
            "exec",
            "execute",
            "union",
            "having"
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SqlInjectionDetector class.
        /// </summary>
        /// <param name="logger">The logger instance for logging detected injection attempts.</param>
        public SqlInjectionDetector(ILogger<SqlInjectionDetector> logger)
        {
            _logger = logger;
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Checks if the input string contains SQL injection patterns.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <returns>True if SQL injection patterns are detected, false otherwise.</returns>
        public bool ContainsSqlInjectionPatterns(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            var lowerInput = input.ToLowerInvariant();

            // Check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (lowerInput.Contains(pattern))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the input string contains SQL injection patterns and logs the attempt if detected.
        /// </summary>
        /// <param name="input">The input string to check.</param>
        /// <param name="fieldName">The name of the field being validated (for logging).</param>
        /// <param name="ipAddress">The IP address of the request (for logging).</param>
        /// <returns>True if SQL injection patterns are detected, false otherwise.</returns>
        public bool ContainsSqlInjectionPatternsWithLogging(string? input, string? fieldName = null, string? ipAddress = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            var lowerInput = input.ToLowerInvariant();
            string? detectedPattern = null;

            // Check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (lowerInput.Contains(pattern))
                {
                    detectedPattern = pattern;
                    break;
                }
            }

            if (detectedPattern != null)
            {
                // Log the detected injection attempt (Requirement 1.6)
                // Sanitize the input for logging to prevent log injection
                var sanitizedInput = SanitizeForLogging(input);
                var sanitizedFieldName = SanitizeForLogging(fieldName ?? "unknown");
                var sanitizedIpAddress = SanitizeForLogging(ipAddress ?? "unknown");

                _logger.LogWarning(
                    "SQL injection attempt detected. Pattern: {Pattern}, Field: {FieldName}, IP: {IpAddress}, Input (truncated): {Input}",
                    detectedPattern,
                    sanitizedFieldName,
                    sanitizedIpAddress,
                    TruncateForLogging(sanitizedInput, 100));

                return true;
            }

            return false;
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Sanitizes input for logging to prevent log injection attacks.
        /// </summary>
        /// <param name="input">The input to sanitize.</param>
        /// <returns>Sanitized string safe for logging.</returns>
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
        /// Truncates a string for logging purposes.
        /// </summary>
        /// <param name="input">The input to truncate.</param>
        /// <param name="maxLength">Maximum length of the output.</param>
        /// <returns>Truncated string.</returns>
        private static string TruncateForLogging(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            {
                return input;
            }

            return input.Substring(0, maxLength) + "...";
        }

        #endregion
    }
}
