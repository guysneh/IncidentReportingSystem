using IncidentReportingSystem.Application.Abstractions.Attachments;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Infrastructure.Attachments.Sanitization
{
    /// <summary>
    /// Image sanitization service based on SixLabors.ImageSharp.
    /// Removes EXIF/ICC metadata, normalizes orientation and rewrites the file in-place.
    /// Supported formats: image/jpeg, image/png. Other content types are a no-op.
    /// </summary>
    public sealed class ImageSharpSanitizer : IImageSanitizer
    {
        private readonly IAttachmentStorage _storage;
        private readonly ILogger<ImageSharpSanitizer> _logger;

        public ImageSharpSanitizer(IAttachmentStorage storage, ILogger<ImageSharpSanitizer> logger)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<(bool changed, long newLength, string? newContentType)> TrySanitizeAsync(
            string storagePath,
            string contentType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(storagePath));

            // Only JPEG/PNG are supported for now.
            if (!IsSupported(contentType))
                return (false, 0L, null);

            try
            {
                await using var src = await _storage.OpenReadAsync(storagePath, cancellationToken).ConfigureAwait(false);
                using var image = await Image.LoadAsync(src, cancellationToken).ConfigureAwait(false);

                // Normalize orientation & strip metadata
                NormalizeOrientation(image);
                StripMetadata(image);

                await using var ms = new MemoryStream(Math.Max(64 * 1024, (int)(src.CanSeek ? src.Length : 0)));
                string newCt;

                if (IsJpeg(contentType))
                {
                    var enc = new JpegEncoder { Quality = 95 };
                    await image.SaveAsJpegAsync(ms, enc, cancellationToken).ConfigureAwait(false);
                    newCt = "image/jpeg";
                }
                else // PNG
                {
                    var enc = new PngEncoder();
                    await image.SaveAsPngAsync(ms, enc, cancellationToken).ConfigureAwait(false);
                    newCt = "image/png";
                }

                ms.Position = 0;
                await _storage.OverwriteAsync(storagePath, ms, newCt, cancellationToken).ConfigureAwait(false);

                var length = ms.Length;
                return (true, length, newCt);
            }
            catch (Exception ex)
            {
                // Do not block Complete on sanitization failures; log and no-op.
                _logger.LogWarning(ex, "Image sanitization failed for path {Path}. Skipping.", storagePath);
                return (false, 0L, null);
            }
        }

        private static bool IsSupported(string ct) => IsJpeg(ct) || IsPng(ct);
        private static bool IsJpeg(string ct) => ct?.StartsWith("image/jpeg", StringComparison.OrdinalIgnoreCase) == true;
        private static bool IsPng(string ct) => ct?.StartsWith("image/png", StringComparison.OrdinalIgnoreCase) == true;

        private static void StripMetadata(Image img)
        {
            // Remove EXIF and ICC to prevent privacy leaks and reduce size.
            img.Metadata.ExifProfile = null;
            img.Metadata.IccProfile = null;
        }

        private static void NormalizeOrientation(Image img)
        {
            // ImageSharp decodes with orientation applied in most cases; ensure no leftover orientation hints remain.
            if (img.Metadata?.ExifProfile is ExifProfile exif && exif.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Orientation, out _))
            {
                // Remove orientation tag after decode to persist pixels in the correct orientation.
                exif.RemoveValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Orientation);
            }
        }
    }
}
