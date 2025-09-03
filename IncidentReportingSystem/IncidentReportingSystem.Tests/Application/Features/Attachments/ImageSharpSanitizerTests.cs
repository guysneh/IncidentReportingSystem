using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.Sanitization;
using IncidentReportingSystem.Tests.Application.Features.Attachments;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.Tests.Attachments
{
    public sealed class ImageSharpSanitizerTests
    {
        private static byte[] CreateJpegWithExif()
        {
            using var img = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(16, 8);
            img.Metadata.ExifProfile = new SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifProfile();
            img.Metadata.ExifProfile.SetValue(ExifTag.Software, "unit-test");
            // Orientation tag present
            img.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)6);

            using var ms = new MemoryStream();
            img.SaveAsJpeg(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
            return ms.ToArray();
        }

        private static byte[] CreatePng()
        {
            using var img = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(4, 4);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }

        [Fact]
        public async Task Jpeg_Is_Sanitized_Exif_Removed_And_Rewritten()
        {
            var store = new InMemoryAttachmentStorage();
            var path = "incidents/2025/att1/test.jpg";
            var data = CreateJpegWithExif();
            store.Seed(path, data, "image/jpeg");

            var sut = new ImageSharpSanitizer(store, NullLogger<ImageSharpSanitizer>.Instance);
            var (changed, newLen, newCt) = await sut.TrySanitizeAsync(path, "image/jpeg", CancellationToken.None);

            Assert.True(changed);
            Assert.True(newLen > 0);
            Assert.Equal("image/jpeg", newCt);

            var props = await store.TryGetUploadedAsync(path, CancellationToken.None);
            Assert.NotNull(props);
            Assert.Equal("image/jpeg", props!.ContentType);

            await using var s = await store.OpenReadAsync(path, CancellationToken.None);
            using var img = await Image.LoadAsync(s);
            Assert.Null(img.Metadata.ExifProfile); // EXIF stripped
        }

        [Fact]
        public async Task Png_Is_Sanitized_No_Metadata_And_Rewritten()
        {
            var store = new InMemoryAttachmentStorage();
            var path = "incidents/2025/att2/test.png";
            store.Seed(path, CreatePng(), "image/png");

            var sut = new ImageSharpSanitizer(store, NullLogger<ImageSharpSanitizer>.Instance);
            var (changed, newLen, newCt) = await sut.TrySanitizeAsync(path, "image/png", CancellationToken.None);

            Assert.True(changed);
            Assert.True(newLen > 0);
            Assert.Equal("image/png", newCt);
        }

        [Theory]
        [InlineData("application/pdf")]
        [InlineData("image/gif")]
        [InlineData("application/octet-stream")]
        public async Task Unsupported_Types_Are_NoOp(string ct)
        {
            var store = new InMemoryAttachmentStorage();
            var path = "incidents/2025/att3/file.bin";
            store.Seed(path, new byte[] { 1, 2, 3 }, ct);

            var sut = new ImageSharpSanitizer(store, NullLogger<ImageSharpSanitizer>.Instance);
            var (changed, newLen, newCt) = await sut.TrySanitizeAsync(path, ct, CancellationToken.None);

            Assert.False(changed);
            Assert.Equal(0, newLen);
            Assert.Null(newCt);
        }
    }
}
