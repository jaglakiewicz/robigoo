#region Imports

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#endregion

namespace Server.Services
{
    #region Result Classes

    /// <summary>
    /// Represents the result of file validation.
    /// </summary>
    public class FileValidationResult
    {
        /// <summary>
        /// Indicates whether the file validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code for programmatic handling.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// The detected file type based on magic bytes (if validation passed).
        /// </summary>
        public string? DetectedContentType { get; set; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static FileValidationResult Success(string? detectedContentType = null) => new()
        {
            IsValid = true,
            DetectedContentType = detectedContentType
        };

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        public static FileValidationResult Failure(string errorMessage, string errorCode) => new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    #endregion

    #region Interface

    /// <summary>
    /// Interface for file validation service.
    /// Provides server-side validation for file uploads including size, content type, and magic bytes validation.
    /// </summary>
    /// <remarks>
    /// Requirement 6.6: THE Backend SHALL validate file uploads (type, size, content) server-side before processing
    /// 
    /// Property 17: File Upload Validation
    /// For any file upload, the Backend SHALL validate: file size is within configured limits,
    /// content type matches allowed types, and file content matches the declared type (magic bytes validation).
    /// </remarks>
    public interface IFileValidationService
    {
        /// <summary>
        /// Validates a file upload for size, content type, and magic bytes.
        /// </summary>
        /// <param name="file">The uploaded file to validate.</param>
        /// <param name="allowedContentTypes">Array of allowed MIME content types.</param>
        /// <param name="maxSizeBytes">Maximum allowed file size in bytes.</param>
        /// <returns>Validation result indicating success or failure with details.</returns>
        Task<FileValidationResult> ValidateFileAsync(IFormFile file, string[] allowedContentTypes, long maxSizeBytes);

        /// <summary>
        /// Validates file content against magic bytes to verify the actual file type.
        /// </summary>
        /// <param name="fileBytes">The file content as byte array.</param>
        /// <param name="declaredContentType">The content type declared by the client.</param>
        /// <returns>Validation result indicating if the magic bytes match the declared type.</returns>
        FileValidationResult ValidateMagicBytes(byte[] fileBytes, string declaredContentType);

        /// <summary>
        /// Detects the file type based on magic bytes.
        /// </summary>
        /// <param name="fileBytes">The file content as byte array.</param>
        /// <returns>The detected MIME type, or null if unknown.</returns>
        string? DetectFileType(byte[] fileBytes);

        /// <summary>
        /// Validates file size against the maximum allowed size.
        /// </summary>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="maxSizeBytes">Maximum allowed file size in bytes.</param>
        /// <returns>Validation result indicating if the size is within limits.</returns>
        FileValidationResult ValidateFileSize(long fileSize, long maxSizeBytes);

        /// <summary>
        /// Validates content type against allowed types.
        /// </summary>
        /// <param name="contentType">The content type to validate.</param>
        /// <param name="allowedContentTypes">Array of allowed MIME content types.</param>
        /// <returns>Validation result indicating if the content type is allowed.</returns>
        FileValidationResult ValidateContentType(string contentType, string[] allowedContentTypes);
    }

    #endregion

    /// <summary>
    /// Service for validating file uploads with comprehensive security checks.
    /// Implements defense-in-depth by validating file size, content type, and magic bytes.
    /// </summary>
    /// <remarks>
    /// Requirement 6.6: THE Backend SHALL validate file uploads (type, size, content) server-side before processing
    /// 
    /// Property 17: File Upload Validation
    /// For any file upload, the Backend SHALL validate: file size is within configured limits,
    /// content type matches allowed types, and file content matches the declared type (magic bytes validation).
    /// 
    /// Magic bytes (file signatures) are the first few bytes of a file that identify its type.
    /// This prevents attackers from uploading malicious files with spoofed content types.
    /// </remarks>
    public class FileValidationService : IFileValidationService
    {
        #region Declarations

        private readonly ILogger<FileValidationService> _logger;

        /// <summary>
        /// Magic bytes signatures for supported file types.
        /// Key: MIME content type, Value: Array of possible magic byte signatures
        /// </summary>
        private static readonly Dictionary<string, byte[][]> MagicBytesSignatures = new()
        {
            // JPEG: FF D8 FF (followed by E0, E1, E2, E3, E8, DB, or EE)
            ["image/jpeg"] = new[]
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, // JFIF
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, // EXIF
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 }, // ICC Profile
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 }, // JIF
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }, // SPIFF
                new byte[] { 0xFF, 0xD8, 0xFF, 0xDB }, // Raw JPEG
                new byte[] { 0xFF, 0xD8, 0xFF, 0xEE }, // Adobe JPEG
                new byte[] { 0xFF, 0xD8, 0xFF }        // Generic JPEG (3 bytes)
            },
            
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            ["image/png"] = new[]
            {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            },
            
            // GIF: 47 49 46 38 (GIF87a or GIF89a)
            ["image/gif"] = new[]
            {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }  // GIF89a
            }
        };

        /// <summary>
        /// Minimum number of bytes required to detect file type.
        /// </summary>
        private const int MinBytesForDetection = 8;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the FileValidationService.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public FileValidationService(ILogger<FileValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Validates a file upload for size, content type, and magic bytes.
        /// </summary>
        /// <param name="file">The uploaded file to validate.</param>
        /// <param name="allowedContentTypes">Array of allowed MIME content types.</param>
        /// <param name="maxSizeBytes">Maximum allowed file size in bytes.</param>
        /// <returns>Validation result indicating success or failure with details.</returns>
        /// <remarks>
        /// Requirement 6.6: THE Backend SHALL validate file uploads (type, size, content) server-side before processing
        /// 
        /// Property 17: File Upload Validation
        /// For any file upload, the Backend SHALL validate: file size is within configured limits,
        /// content type matches allowed types, and file content matches the declared type (magic bytes validation).
        /// </remarks>
        public async Task<FileValidationResult> ValidateFileAsync(
            IFormFile file, 
            string[] allowedContentTypes, 
            long maxSizeBytes)
        {
            // Validate file presence
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File validation failed: No file provided or file is empty");
                return FileValidationResult.Failure(
                    "Plik nie został przesłany lub jest pusty",
                    "FILE_EMPTY");
            }

            // Validate file size
            var sizeResult = ValidateFileSize(file.Length, maxSizeBytes);
            if (!sizeResult.IsValid)
            {
                _logger.LogWarning(
                    "File validation failed: Size {FileSize} exceeds maximum {MaxSize}",
                    file.Length, maxSizeBytes);
                return sizeResult;
            }

            // Validate declared content type
            var contentTypeResult = ValidateContentType(file.ContentType, allowedContentTypes);
            if (!contentTypeResult.IsValid)
            {
                _logger.LogWarning(
                    "File validation failed: Content type {ContentType} not allowed",
                    file.ContentType);
                return contentTypeResult;
            }

            // Read file bytes for magic bytes validation
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Validate magic bytes match declared content type
            var magicBytesResult = ValidateMagicBytes(fileBytes, file.ContentType);
            if (!magicBytesResult.IsValid)
            {
                _logger.LogWarning(
                    "File validation failed: Magic bytes do not match declared content type {ContentType}. Detected type: {DetectedType}",
                    file.ContentType, magicBytesResult.DetectedContentType ?? "unknown");
                return magicBytesResult;
            }

            _logger.LogDebug(
                "File validation passed: Size={Size}, ContentType={ContentType}, DetectedType={DetectedType}",
                file.Length, file.ContentType, magicBytesResult.DetectedContentType);

            return FileValidationResult.Success(magicBytesResult.DetectedContentType);
        }

        /// <summary>
        /// Validates file content against magic bytes to verify the actual file type.
        /// </summary>
        /// <param name="fileBytes">The file content as byte array.</param>
        /// <param name="declaredContentType">The content type declared by the client.</param>
        /// <returns>Validation result indicating if the magic bytes match the declared type.</returns>
        /// <remarks>
        /// Magic bytes validation prevents attackers from uploading malicious files
        /// with spoofed content types (e.g., uploading an executable as image/jpeg).
        /// </remarks>
        public FileValidationResult ValidateMagicBytes(byte[] fileBytes, string declaredContentType)
        {
            if (fileBytes == null || fileBytes.Length < MinBytesForDetection)
            {
                return FileValidationResult.Failure(
                    "Plik jest za mały do weryfikacji typu",
                    "FILE_TOO_SMALL");
            }

            // Detect the actual file type from magic bytes
            var detectedType = DetectFileType(fileBytes);

            // If we couldn't detect the type, the file format is not supported
            if (detectedType == null)
            {
                return FileValidationResult.Failure(
                    "Nie można zweryfikować typu pliku. Obsługiwane formaty: JPEG, PNG, GIF",
                    "UNKNOWN_FILE_TYPE");
            }

            // Normalize content types for comparison
            var normalizedDeclared = NormalizeContentType(declaredContentType);
            var normalizedDetected = NormalizeContentType(detectedType);

            // Check if detected type matches declared type
            if (!string.Equals(normalizedDeclared, normalizedDetected, StringComparison.OrdinalIgnoreCase))
            {
                var result = FileValidationResult.Failure(
                    $"Typ pliku ({detectedType}) nie odpowiada zadeklarowanemu typowi ({declaredContentType})",
                    "CONTENT_TYPE_MISMATCH");
                result.DetectedContentType = detectedType;
                return result;
            }

            return FileValidationResult.Success(detectedType);
        }

        /// <summary>
        /// Detects the file type based on magic bytes.
        /// </summary>
        /// <param name="fileBytes">The file content as byte array.</param>
        /// <returns>The detected MIME type, or null if unknown.</returns>
        public string? DetectFileType(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < MinBytesForDetection)
            {
                return null;
            }

            foreach (var (contentType, signatures) in MagicBytesSignatures)
            {
                foreach (var signature in signatures)
                {
                    if (StartsWithSignature(fileBytes, signature))
                    {
                        return contentType;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Validates file size against the maximum allowed size.
        /// </summary>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="maxSizeBytes">Maximum allowed file size in bytes.</param>
        /// <returns>Validation result indicating if the size is within limits.</returns>
        public FileValidationResult ValidateFileSize(long fileSize, long maxSizeBytes)
        {
            if (fileSize <= 0)
            {
                return FileValidationResult.Failure(
                    "Plik jest pusty",
                    "FILE_EMPTY");
            }

            if (fileSize > maxSizeBytes)
            {
                var maxSizeMB = maxSizeBytes / (1024.0 * 1024.0);
                return FileValidationResult.Failure(
                    $"Plik jest za duży. Maksymalny rozmiar to {maxSizeMB:F1}MB",
                    "FILE_TOO_LARGE");
            }

            return FileValidationResult.Success();
        }

        /// <summary>
        /// Validates content type against allowed types.
        /// </summary>
        /// <param name="contentType">The content type to validate.</param>
        /// <param name="allowedContentTypes">Array of allowed MIME content types.</param>
        /// <returns>Validation result indicating if the content type is allowed.</returns>
        public FileValidationResult ValidateContentType(string contentType, string[] allowedContentTypes)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return FileValidationResult.Failure(
                    "Typ pliku nie został określony",
                    "MISSING_CONTENT_TYPE");
            }

            if (allowedContentTypes == null || allowedContentTypes.Length == 0)
            {
                return FileValidationResult.Failure(
                    "Brak zdefiniowanych dozwolonych typów plików",
                    "NO_ALLOWED_TYPES");
            }

            var normalizedContentType = NormalizeContentType(contentType);
            var isAllowed = allowedContentTypes.Any(allowed => 
                string.Equals(NormalizeContentType(allowed), normalizedContentType, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                var allowedTypesStr = string.Join(", ", allowedContentTypes.Select(GetFriendlyTypeName));
                return FileValidationResult.Failure(
                    $"Obsługiwane są tylko pliki: {allowedTypesStr}",
                    "INVALID_CONTENT_TYPE");
            }

            return FileValidationResult.Success();
        }

        #endregion

        #region Methods - Private

        /// <summary>
        /// Checks if the file bytes start with the given signature.
        /// </summary>
        private static bool StartsWithSignature(byte[] fileBytes, byte[] signature)
        {
            if (fileBytes.Length < signature.Length)
            {
                return false;
            }

            for (int i = 0; i < signature.Length; i++)
            {
                if (fileBytes[i] != signature[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Normalizes a content type by removing parameters (e.g., charset).
        /// </summary>
        private static string NormalizeContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return string.Empty;
            }

            // Remove any parameters (e.g., "image/jpeg; charset=utf-8" -> "image/jpeg")
            var semicolonIndex = contentType.IndexOf(';');
            if (semicolonIndex > 0)
            {
                contentType = contentType.Substring(0, semicolonIndex);
            }

            return contentType.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Gets a user-friendly name for a content type.
        /// </summary>
        private static string GetFriendlyTypeName(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => "JPEG",
                "image/png" => "PNG",
                "image/gif" => "GIF",
                "application/pdf" => "PDF",
                _ => contentType
            };
        }

        #endregion
    }
}
