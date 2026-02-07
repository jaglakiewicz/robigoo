/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Extensions
{
    /// <summary>
    /// Extension methods for controllers to easily return secure file downloads.
    /// Implements requirement 10.6: WHEN serving file downloads, THE Backend SHALL include Content-Disposition headers.
    /// 
    /// Usage example:
    /// <code>
    /// public async Task&lt;IActionResult&gt; DownloadReport(long id)
    /// {
    ///     var reportData = await _reportService.GenerateReportAsync(id);
    ///     return this.SecureFileDownload(
    ///         _fileDownloadService,
    ///         reportData,
    ///         $"report_{id}.pdf",
    ///         "application/pdf");
    /// }
    /// </code>
    /// </summary>
    public static class ControllerFileDownloadExtensions
    {
        /// <summary>
        /// Returns a file download with proper Content-Disposition headers and sanitized filename.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="fileDownloadService">The file download service for secure file handling.</param>
        /// <param name="content">The file content as byte array.</param>
        /// <param name="fileName">The filename to suggest to the client (will be sanitized).</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="inline">If true, suggests browser display inline; if false, suggests download.</param>
        /// <returns>A FileContentResult with proper security headers.</returns>
        public static FileContentResult SecureFileDownload(
            this ControllerBase controller,
            IFileDownloadService fileDownloadService,
            byte[] content,
            string fileName,
            string contentType,
            bool inline = false)
        {
            if (fileDownloadService == null)
            {
                throw new ArgumentNullException(nameof(fileDownloadService));
            }

            return fileDownloadService.CreateFileDownload(content, fileName, contentType, inline);
        }

        /// <summary>
        /// Returns a file stream download with proper Content-Disposition headers and sanitized filename.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="fileDownloadService">The file download service for secure file handling.</param>
        /// <param name="stream">The file content stream.</param>
        /// <param name="fileName">The filename to suggest to the client (will be sanitized).</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="inline">If true, suggests browser display inline; if false, suggests download.</param>
        /// <returns>A FileStreamResult with proper security headers.</returns>
        public static FileStreamResult SecureFileDownload(
            this ControllerBase controller,
            IFileDownloadService fileDownloadService,
            Stream stream,
            string fileName,
            string contentType,
            bool inline = false)
        {
            if (fileDownloadService == null)
            {
                throw new ArgumentNullException(nameof(fileDownloadService));
            }

            return fileDownloadService.CreateFileDownload(stream, fileName, contentType, inline);
        }
    }
}
