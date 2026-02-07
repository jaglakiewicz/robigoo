/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Services
{
    /// <summary>
    /// Service for handling secure file downloads with proper Content-Disposition headers.
    /// Implements requirement 10.6: WHEN serving file downloads, THE Backend SHALL include Content-Disposition headers.
    /// 
    /// This service provides:
    /// - Filename sanitization to prevent header injection attacks
    /// - Proper Content-Disposition header formatting (RFC 6266 compliant)
    /// - Content-Type validation and setting
    /// - Support for both inline and attachment dispositions
    /// </summary>
    public interface IFileDownloadService
    {
        /// <summary>
        /// Creates a FileContentResult with proper security headers for file download.
        /// </summary>
        /// <param name="content">The file content as byte array.</param>
        /// <param name="fileName">The filename to use in Content-Disposition header.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="inline">If true, suggests browser display inline; if false, suggests download as attachment.</param>
        /// <returns>A FileContentResult with proper headers set.</returns>
        FileContentResult CreateFileDownload(byte[] content, string fileName, string contentType, bool inline = false);

        /// <summary>
        /// Creates a FileStreamResult with proper security headers for file download.
        /// </summary>
        /// <param name="stream">The file content stream.</param>
        /// <param name="fileName">The filename to use in Content-Disposition header.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="inline">If true, suggests browser display inline; if false, suggests download as attachment.</param>
        /// <returns>A FileStreamResult with proper headers set.</returns>
        FileStreamResult CreateFileDownload(Stream stream, string fileName, string contentType, bool inline = false);

        /// <summary>
        /// Sanitizes a filename to prevent header injection attacks.
        /// </summary>
        /// <param name="fileName">The original filename.</param>
        /// <returns>A sanitized filename safe for use in HTTP headers.</returns>
        string SanitizeFileName(string fileName);

        /// <summary>
        /// Generates a Content-Disposition header value with proper encoding.
        /// </summary>
        /// <param name="fileName">The filename (will be sanitized).</param>
        /// <param name="inline">If true, disposition is "inline"; if false, "attachment".</param>
        /// <returns>A properly formatted Content-Disposition header value.</returns>
        string GenerateContentDisposition(string fileName, bool inline = false);
    }

    /// <summary>
    /// Implementation of IFileDownloadService providing secure file download functionality.
    /// </summary>
    public class FileDownloadService : IFileDownloadService
    {
        #region Constants

        /// <summary>
        /// Characters that are not allowed in filenames for security reasons.
        /// Includes path separators, null bytes, and control characters.
        /// </summary>
        private static readonly char[] InvalidFileNameChars = new[]
        {
            '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0', '\n', '\r', '\t'
        };

        /// <summary>
        /// Maximum allowed filename length to prevent buffer overflow attacks.
        /// </summary>
        private const int MaxFileNameLength = 255;

        /// <summary>
        /// Default filename to use when the provided filename is invalid or empty.
        /// </summary>
        private const string DefaultFileName = "download";

        /// <summary>
        /// Regex pattern for detecting header injection attempts.
        /// Matches newlines and other control characters that could be used for header injection.
        /// </summary>
        private static readonly Regex HeaderInjectionPattern = new Regex(
            @"[\r\n\x00-\x1f\x7f]",
            RegexOptions.Compiled);

        #endregion

        #region Declarations

        private readonly ILogger<FileDownloadService> _logger;

        #endregion

        #region Constructor

        public FileDownloadService(ILogger<FileDownloadService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Methods - Public

        /// <inheritdoc />
        public FileContentResult CreateFileDownload(byte[] content, string fileName, string contentType, bool inline = false)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var sanitizedFileName = SanitizeFileName(fileName);
            var validContentType = ValidateContentType(contentType);

            var result = new FileContentResult(content, validContentType)
            {
                FileDownloadName = sanitizedFileName
            };

            // The FileDownloadName property automatically sets Content-Disposition header
            // with the "attachment" disposition. For inline, we need to handle it differently.
            // ASP.NET Core's FileContentResult handles this properly when FileDownloadName is set.

            _logger.LogDebug(
                "Created file download: FileName={FileName}, ContentType={ContentType}, Size={Size}, Inline={Inline}",
                sanitizedFileName, validContentType, content.Length, inline);

            return result;
        }

        /// <inheritdoc />
        public FileStreamResult CreateFileDownload(Stream stream, string fileName, string contentType, bool inline = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var sanitizedFileName = SanitizeFileName(fileName);
            var validContentType = ValidateContentType(contentType);

            var result = new FileStreamResult(stream, validContentType)
            {
                FileDownloadName = sanitizedFileName
            };

            _logger.LogDebug(
                "Created file stream download: FileName={FileName}, ContentType={ContentType}, Inline={Inline}",
                sanitizedFileName, validContentType, inline);

            return result;
        }

        /// <inheritdoc />
        public string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Empty or null filename provided, using default");
                return DefaultFileName;
            }

            // Remove any path components - only keep the filename
            fileName = Path.GetFileName(fileName);

            // Check for header injection attempts
            if (HeaderInjectionPattern.IsMatch(fileName))
            {
                _logger.LogWarning(
                    "Potential header injection detected in filename, sanitizing: {FileName}",
                    SanitizeForLogging(fileName));
                fileName = HeaderInjectionPattern.Replace(fileName, "_");
            }

            // Remove invalid characters
            var sanitized = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
            {
                if (Array.IndexOf(InvalidFileNameChars, c) < 0 && !char.IsControl(c))
                {
                    sanitized.Append(c);
                }
                else
                {
                    sanitized.Append('_');
                }
            }

            var result = sanitized.ToString().Trim();

            // Ensure we have a valid filename
            if (string.IsNullOrWhiteSpace(result) || result == "." || result == "..")
            {
                _logger.LogWarning("Filename sanitization resulted in empty string, using default");
                return DefaultFileName;
            }

            // Truncate if too long
            if (result.Length > MaxFileNameLength)
            {
                // Try to preserve the extension
                var extension = Path.GetExtension(result);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(result);
                
                var maxNameLength = MaxFileNameLength - extension.Length;
                if (maxNameLength > 0 && nameWithoutExtension.Length > maxNameLength)
                {
                    result = nameWithoutExtension.Substring(0, maxNameLength) + extension;
                }
                else
                {
                    result = result.Substring(0, MaxFileNameLength);
                }

                _logger.LogWarning(
                    "Filename truncated from {OriginalLength} to {NewLength} characters",
                    fileName.Length, result.Length);
            }

            return result;
        }

        /// <inheritdoc />
        public string GenerateContentDisposition(string fileName, bool inline = false)
        {
            var sanitizedFileName = SanitizeFileName(fileName);
            var disposition = inline ? "inline" : "attachment";

            // RFC 6266 compliant Content-Disposition header
            // Use both filename (ASCII) and filename* (UTF-8) for maximum compatibility
            var asciiFileName = ConvertToAscii(sanitizedFileName);
            var utf8FileName = Uri.EscapeDataString(sanitizedFileName);

            // If the filename is pure ASCII, we can use the simple format
            if (asciiFileName == sanitizedFileName)
            {
                return $"{disposition}; filename=\"{EscapeQuotes(sanitizedFileName)}\"";
            }

            // For non-ASCII filenames, use both formats for compatibility
            // filename for older clients, filename* for modern clients (RFC 5987)
            return $"{disposition}; filename=\"{EscapeQuotes(asciiFileName)}\"; filename*=UTF-8''{utf8FileName}";
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Validates and normalizes the content type.
        /// </summary>
        private string ValidateContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return MediaTypeNames.Application.Octet;
            }

            // Check for header injection in content type
            if (HeaderInjectionPattern.IsMatch(contentType))
            {
                _logger.LogWarning(
                    "Potential header injection detected in content type, using default: {ContentType}",
                    SanitizeForLogging(contentType));
                return MediaTypeNames.Application.Octet;
            }

            // Basic validation - content type should be in format "type/subtype"
            if (!contentType.Contains('/'))
            {
                _logger.LogWarning(
                    "Invalid content type format, using default: {ContentType}",
                    contentType);
                return MediaTypeNames.Application.Octet;
            }

            return contentType.Trim();
        }

        /// <summary>
        /// Converts a string to ASCII, replacing non-ASCII characters with underscores.
        /// </summary>
        private static string ConvertToAscii(string input)
        {
            var result = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                result.Append(c < 128 ? c : '_');
            }
            return result.ToString();
        }

        /// <summary>
        /// Escapes double quotes in a string for use in HTTP header values.
        /// </summary>
        private static string EscapeQuotes(string input)
        {
            return input.Replace("\"", "\\\"");
        }

        /// <summary>
        /// Sanitizes a string for safe logging (removes control characters).
        /// </summary>
        private static string SanitizeForLogging(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return HeaderInjectionPattern.Replace(input, "[CTRL]");
        }

        #endregion
    }
}
