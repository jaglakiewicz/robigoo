/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Services;
using Xunit;

namespace Server.Tests
{
    /// <summary>
    /// Unit tests for FileValidationService.
    /// Tests file upload validation including size, content type, and magic bytes validation.
    /// 
    /// Requirement 6.6: THE Backend SHALL validate file uploads (type, size, content) server-side before processing
    /// 
    /// Property 17: File Upload Validation
    /// For any file upload, the Backend SHALL validate: file size is within configured limits,
    /// content type matches allowed types, and file content matches the declared type (magic bytes validation).
    /// </summary>
    public class FileValidationServiceTests
    {
        private readonly FileValidationService _service;
        private readonly Mock<ILogger<FileValidationService>> _loggerMock;

        // Magic bytes for test files
        private static readonly byte[] JpegMagicBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
        private static readonly byte[] PngMagicBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] GifMagicBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00 };
        private static readonly byte[] InvalidMagicBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        private static readonly string[] AllowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif" };

        public FileValidationServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileValidationService>>();
            _service = new FileValidationService(_loggerMock.Object);
        }

        #region DetectFileType Tests

        [Fact]
        public void DetectFileType_WithJpegMagicBytes_ReturnsImageJpeg()
        {
            // Arrange
            var fileBytes = JpegMagicBytes;

            // Act
            var result = _service.DetectFileType(fileBytes);

            // Assert
            Assert.Equal("image/jpeg", result);
        }

        [Fact]
        public void DetectFileType_WithPngMagicBytes_ReturnsImagePng()
        {
            // Arrange
            var fileBytes = PngMagicBytes;

            // Act
            var result = _service.DetectFileType(fileBytes);

            // Assert
            Assert.Equal("image/png", result);
        }

        [Fact]
        public void DetectFileType_WithGifMagicBytes_ReturnsImageGif()
        {
            // Arrange
            var fileBytes = GifMagicBytes;

            // Act
            var result = _service.DetectFileType(fileBytes);

            // Assert
            Assert.Equal("image/gif", result);
        }

        [Fact]
        public void DetectFileType_WithUnknownMagicBytes_ReturnsNull()
        {
            // Arrange
            var fileBytes = InvalidMagicBytes;

            // Act
            var result = _service.DetectFileType(fileBytes);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DetectFileType_WithNullBytes_ReturnsNull()
        {
            // Act
            var result = _service.DetectFileType(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void DetectFileType_WithTooFewBytes_ReturnsNull()
        {
            // Arrange - less than 8 bytes
            var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF };

            // Act
            var result = _service.DetectFileType(fileBytes);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region ValidateMagicBytes Tests

        [Fact]
        public void ValidateMagicBytes_WithMatchingJpegType_ReturnsSuccess()
        {
            // Arrange
            var fileBytes = JpegMagicBytes;

            // Act
            var result = _service.ValidateMagicBytes(fileBytes, "image/jpeg");

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("image/jpeg", result.DetectedContentType);
        }

        [Fact]
        public void ValidateMagicBytes_WithMatchingPngType_ReturnsSuccess()
        {
            // Arrange
            var fileBytes = PngMagicBytes;

            // Act
            var result = _service.ValidateMagicBytes(fileBytes, "image/png");

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("image/png", result.DetectedContentType);
        }

        [Fact]
        public void ValidateMagicBytes_WithMatchingGifType_ReturnsSuccess()
        {
            // Arrange
            var fileBytes = GifMagicBytes;

            // Act
            var result = _service.ValidateMagicBytes(fileBytes, "image/gif");

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("image/gif", result.DetectedContentType);
        }

        [Fact]
        public void ValidateMagicBytes_WithMismatchedType_ReturnsFailure()
        {
            // Arrange - PNG bytes but claiming to be JPEG
            var fileBytes = PngMagicBytes;

            // Act
            var result = _service.ValidateMagicBytes(fileBytes, "image/jpeg");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("CONTENT_TYPE_MISMATCH", result.ErrorCode);
            Assert.Equal("image/png", result.DetectedContentType);
        }

        [Fact]
        public void ValidateMagicBytes_WithUnknownFileType_ReturnsFailure()
        {
            // Arrange
            var fileBytes = InvalidMagicBytes;

            // Act
            var result = _service.ValidateMagicBytes(fileBytes, "image/jpeg");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("UNKNOWN_FILE_TYPE", result.ErrorCode);
        }

        [Fact]
        public void ValidateMagicBytes_WithTooSmallFile_ReturnsFailure()
        {
            // Arrange
            var fileBytes = new byte[] { 0xFF, 0xD8 };

            // Act
            var result = _service.ValidateMagicBytes(fileBytes, "image/jpeg");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_TOO_SMALL", result.ErrorCode);
        }

        #endregion

        #region ValidateFileSize Tests

        [Fact]
        public void ValidateFileSize_WithinLimit_ReturnsSuccess()
        {
            // Arrange
            long fileSize = 1024 * 1024; // 1MB
            long maxSize = 5 * 1024 * 1024; // 5MB

            // Act
            var result = _service.ValidateFileSize(fileSize, maxSize);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateFileSize_ExactlyAtLimit_ReturnsSuccess()
        {
            // Arrange
            long fileSize = 5 * 1024 * 1024; // 5MB
            long maxSize = 5 * 1024 * 1024; // 5MB

            // Act
            var result = _service.ValidateFileSize(fileSize, maxSize);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateFileSize_ExceedsLimit_ReturnsFailure()
        {
            // Arrange
            long fileSize = 6 * 1024 * 1024; // 6MB
            long maxSize = 5 * 1024 * 1024; // 5MB

            // Act
            var result = _service.ValidateFileSize(fileSize, maxSize);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_TOO_LARGE", result.ErrorCode);
        }

        [Fact]
        public void ValidateFileSize_ZeroSize_ReturnsFailure()
        {
            // Arrange
            long fileSize = 0;
            long maxSize = 5 * 1024 * 1024;

            // Act
            var result = _service.ValidateFileSize(fileSize, maxSize);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_EMPTY", result.ErrorCode);
        }

        [Fact]
        public void ValidateFileSize_NegativeSize_ReturnsFailure()
        {
            // Arrange
            long fileSize = -1;
            long maxSize = 5 * 1024 * 1024;

            // Act
            var result = _service.ValidateFileSize(fileSize, maxSize);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_EMPTY", result.ErrorCode);
        }

        #endregion

        #region ValidateContentType Tests

        [Fact]
        public void ValidateContentType_AllowedType_ReturnsSuccess()
        {
            // Act
            var result = _service.ValidateContentType("image/jpeg", AllowedImageTypes);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateContentType_AllowedTypeWithDifferentCase_ReturnsSuccess()
        {
            // Act
            var result = _service.ValidateContentType("IMAGE/JPEG", AllowedImageTypes);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidateContentType_NotAllowedType_ReturnsFailure()
        {
            // Act
            var result = _service.ValidateContentType("application/pdf", AllowedImageTypes);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_CONTENT_TYPE", result.ErrorCode);
        }

        [Fact]
        public void ValidateContentType_EmptyContentType_ReturnsFailure()
        {
            // Act
            var result = _service.ValidateContentType("", AllowedImageTypes);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("MISSING_CONTENT_TYPE", result.ErrorCode);
        }

        [Fact]
        public void ValidateContentType_NullContentType_ReturnsFailure()
        {
            // Act
            var result = _service.ValidateContentType(null!, AllowedImageTypes);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("MISSING_CONTENT_TYPE", result.ErrorCode);
        }

        [Fact]
        public void ValidateContentType_EmptyAllowedTypes_ReturnsFailure()
        {
            // Act
            var result = _service.ValidateContentType("image/jpeg", Array.Empty<string>());

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("NO_ALLOWED_TYPES", result.ErrorCode);
        }

        [Fact]
        public void ValidateContentType_ContentTypeWithParameters_ReturnsSuccess()
        {
            // Arrange - content type with charset parameter
            var contentType = "image/jpeg; charset=utf-8";

            // Act
            var result = _service.ValidateContentType(contentType, AllowedImageTypes);

            // Assert
            Assert.True(result.IsValid);
        }

        #endregion

        #region ValidateFileAsync Tests

        [Fact]
        public async Task ValidateFileAsync_ValidJpegFile_ReturnsSuccess()
        {
            // Arrange
            var fileContent = CreateValidJpegContent();
            var file = CreateMockFormFile(fileContent, "test.jpg", "image/jpeg");

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("image/jpeg", result.DetectedContentType);
        }

        [Fact]
        public async Task ValidateFileAsync_ValidPngFile_ReturnsSuccess()
        {
            // Arrange
            var fileContent = CreateValidPngContent();
            var file = CreateMockFormFile(fileContent, "test.png", "image/png");

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("image/png", result.DetectedContentType);
        }

        [Fact]
        public async Task ValidateFileAsync_ValidGifFile_ReturnsSuccess()
        {
            // Arrange
            var fileContent = CreateValidGifContent();
            var file = CreateMockFormFile(fileContent, "test.gif", "image/gif");

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("image/gif", result.DetectedContentType);
        }

        [Fact]
        public async Task ValidateFileAsync_FileTooLarge_ReturnsFailure()
        {
            // Arrange
            var fileContent = CreateValidJpegContent();
            var file = CreateMockFormFile(fileContent, "test.jpg", "image/jpeg", 6 * 1024 * 1024);

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_TOO_LARGE", result.ErrorCode);
        }

        [Fact]
        public async Task ValidateFileAsync_InvalidContentType_ReturnsFailure()
        {
            // Arrange
            var fileContent = CreateValidJpegContent();
            var file = CreateMockFormFile(fileContent, "test.pdf", "application/pdf");

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("INVALID_CONTENT_TYPE", result.ErrorCode);
        }

        [Fact]
        public async Task ValidateFileAsync_MismatchedMagicBytes_ReturnsFailure()
        {
            // Arrange - PNG content but claiming to be JPEG
            var fileContent = CreateValidPngContent();
            var file = CreateMockFormFile(fileContent, "test.jpg", "image/jpeg");

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("CONTENT_TYPE_MISMATCH", result.ErrorCode);
        }

        [Fact]
        public async Task ValidateFileAsync_NullFile_ReturnsFailure()
        {
            // Act
            var result = await _service.ValidateFileAsync(null!, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_EMPTY", result.ErrorCode);
        }

        [Fact]
        public async Task ValidateFileAsync_EmptyFile_ReturnsFailure()
        {
            // Arrange
            var file = CreateMockFormFile(Array.Empty<byte>(), "test.jpg", "image/jpeg", 0);

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("FILE_EMPTY", result.ErrorCode);
        }

        [Fact]
        public async Task ValidateFileAsync_UnknownFileType_ReturnsFailure()
        {
            // Arrange - random bytes that don't match any known file type
            var fileContent = InvalidMagicBytes;
            var file = CreateMockFormFile(fileContent, "test.jpg", "image/jpeg");

            // Act
            var result = await _service.ValidateFileAsync(file, AllowedImageTypes, 5 * 1024 * 1024);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("UNKNOWN_FILE_TYPE", result.ErrorCode);
        }

        #endregion

        #region Helper Methods

        private static byte[] CreateValidJpegContent()
        {
            // Create a minimal valid JPEG-like content with magic bytes
            var content = new byte[100];
            JpegMagicBytes.CopyTo(content, 0);
            return content;
        }

        private static byte[] CreateValidPngContent()
        {
            // Create a minimal valid PNG-like content with magic bytes
            var content = new byte[100];
            PngMagicBytes.CopyTo(content, 0);
            return content;
        }

        private static byte[] CreateValidGifContent()
        {
            // Create a minimal valid GIF-like content with magic bytes
            var content = new byte[100];
            GifMagicBytes.CopyTo(content, 0);
            return content;
        }

        private static IFormFile CreateMockFormFile(byte[] content, string fileName, string contentType, long? overrideLength = null)
        {
            var stream = new MemoryStream(content);
            var fileMock = new Mock<IFormFile>();
            
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(overrideLength ?? content.Length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((target, _) =>
                {
                    stream.Position = 0;
                    stream.CopyTo(target);
                })
                .Returns(Task.CompletedTask);

            return fileMock.Object;
        }

        #endregion
    }
}
