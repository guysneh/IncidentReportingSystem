using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Tests.Application.Features.Attachments
{
    /// <summary>
    /// Unit tests for the development loopback storage:
    /// - async I/O
    /// - path normalization/validation
    /// - content-type normalization from extension
    /// </summary>
    [Trait("Category", "Unit")]
    public sealed class LoopbackAttachmentStorageTests
    {
        private static LoopbackAttachmentStorage CreateStorage(string basePath = "/api")
        {
            var uniqueRoot = Path.Combine(Path.GetTempPath(), "irs-loopback-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(uniqueRoot);

            var dict = new Dictionary<string, string?>
            {
                ["Api:PublicBaseUrl"] = "https://localhost:7041",
                ["Api:DefaultVersion"] = "v1",
                ["Api:BasePath"] = basePath,
                ["Attachments:Loopback:Root"] = uniqueRoot
            };

            var cfg = new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
            return new LoopbackAttachmentStorage(cfg);
        }

        [Fact(DisplayName = "CreateUploadSlot builds URL that respects BasePath and avoids double /api")]
        public async Task CreateUploadSlot_Builds_Expected_UploadUrl()
        {
            var storage = CreateStorage(basePath: "/api");

            var req = new CreateUploadSlotRequest(
                ContentType: "image/jpeg",
                PathPrefix: "incidents/6/i",
                AttachmentId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                FileName: "file.jpg");

            var slot = await storage.CreateUploadSlotAsync(req, CancellationToken.None);

            slot.StoragePath.Should().Be("incidents/6/i/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/file.jpg");
            slot.UploadUrl.Should().NotBeNull();
            slot.UploadUrl.AbsoluteUri.Should().Be("https://localhost:7041/api/v1/attachments/_loopback/upload?path=incidents%2F6%2Fi%2Faaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa%2Ffile.jpg");
        }

        [Fact(DisplayName = "ReceiveUpload (binary) stores bytes and infers content-type from extension when octet-stream")]
        public async Task ReceiveUpload_Stores_Bytes_And_Infers_ContentType()
        {
            var storage = CreateStorage();
            var storagePath = "incidents/6/i/00000000-0000-0000-0000-000000000001/pic.jpeg";
            var data = Encoding.UTF8.GetBytes("hello-image");

            await storage.ReceiveUploadAsync(storagePath, new MemoryStream(data), "application/octet-stream", CancellationToken.None);

            var props = await storage.TryGetUploadedAsync(storagePath, CancellationToken.None);
            props.Should().NotBeNull();
            props!.Length.Should().Be(data.Length);
            props.ContentType.Should().Be("image/jpeg");
            props.ETag.Should().NotBeNullOrWhiteSpace();
        }

        [Fact(DisplayName = "OpenRead returns stream with uploaded bytes")]
        public async Task OpenRead_Returns_Bytes()
        {
            var storage = CreateStorage();
            var storagePath = "incidents/6/i/00000000-0000-0000-0000-000000000002/doc.pdf";
            var data = Encoding.UTF8.GetBytes("pdf-bytes");

            await storage.ReceiveUploadAsync(storagePath, new MemoryStream(data), "application/octet-stream", CancellationToken.None);

            await using var s = await storage.OpenReadAsync(storagePath, CancellationToken.None);
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms);
            ms.ToArray().Should().BeEquivalentTo(data);
        }

        [Fact(DisplayName = "Delete removes stored object")]
        public async Task Delete_Removes_Object()
        {
            var storage = CreateStorage();
            var storagePath = "incidents/6/i/00000000-0000-0000-0000-000000000003/pic.png";

            await storage.ReceiveUploadAsync(storagePath, new MemoryStream(new byte[] { 1, 2, 3 }), "image/png", CancellationToken.None);
            (await storage.TryGetUploadedAsync(storagePath, CancellationToken.None)).Should().NotBeNull();

            await storage.DeleteAsync(storagePath, CancellationToken.None);
            (await storage.TryGetUploadedAsync(storagePath, CancellationToken.None)).Should().BeNull();
        }

        [Theory(DisplayName = "ReceiveUpload throws on invalid storage path (full URL, leading slash, traversal)")]
        [InlineData("https://localhost:7041/incidents/1/1/a.jpg")]
        [InlineData("/incidents/1/1/a.jpg")]
        [InlineData("..//incidents/1/1/a.jpg")]
        [InlineData("comments\\1\\1\\a.jpg")]
        public async Task ReceiveUpload_Rejects_Invalid_Path(string badPath)
        {
            var storage = CreateStorage();
            Func<Task> act = () => storage.ReceiveUploadAsync(badPath, new MemoryStream(new byte[] { 1 }), "image/jpeg", CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Invalid storage path*");
        }

        [Fact(DisplayName = "TryGetUploadedAsync returns null for unknown path")]
        public async Task TryGetUnknown_Returns_Null()
        {
            var storage = CreateStorage();
            var props = await storage.TryGetUploadedAsync("incidents/none/none/missing.jpg", CancellationToken.None);
            props.Should().BeNull();
        }
    }
}
