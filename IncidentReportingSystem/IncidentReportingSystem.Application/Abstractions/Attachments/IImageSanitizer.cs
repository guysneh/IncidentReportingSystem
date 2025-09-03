using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>
    /// Sanitizes image files by removing sensitive metadata (e.g., EXIF) and normalizing orientation.
    /// Implementations should be idempotent and safe to run multiple times.
    /// </summary>
    public interface IImageSanitizer
    {
        /// <summary>
        /// Attempts to sanitize an image in-place in the underlying storage.
        /// If the content-type is not supported or no changes are required, the method must be a no-op.
        /// </summary>
        /// <param name="storagePath">Provider-specific opaque path identifying the object.</param>
        /// <param name="contentType">Server-observed MIME type (e.g., image/jpeg or image/png).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// (changed, newLength, newContentType) — where <c>changed</c> indicates that the blob content
        /// has been replaced. <c>newContentType</c> should be preserved if format remained the same.
        /// </returns>
        Task<(bool changed, long newLength, string? newContentType)> TrySanitizeAsync(
            string storagePath,
            string contentType,
            CancellationToken cancellationToken);
    }
}
