using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.Fake;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

public sealed class FakeAttachmentStorageTests
{
    [Fact]
    public async Task CreateSlot_ThenTryGet_BeforeAndAfterSimulatedUpload()
    {
        var store = new FakeAttachmentStorage();
        var req = new CreateUploadSlotRequest(
            AttachmentId: Guid.NewGuid(),
            FileName: "report.pdf",
            ContentType: MediaTypeNames.Application.Pdf,
            PathPrefix: "inc/2025/08");

        var slot = await store.CreateUploadSlotAsync(req, default);

        Assert.Equal($"inc/2025/08/{req.AttachmentId}/report.pdf", slot.StoragePath);
        Assert.StartsWith("https://fake-upload/", slot.UploadUrl.ToString());
        Assert.True(slot.ExpiresAt > DateTimeOffset.UtcNow);

        var before = await store.TryGetUploadedAsync(slot.StoragePath, default);
        Assert.Null(before);

        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        store.SimulateClientUpload(slot.StoragePath, bytes, req.ContentType);

        var after = await store.TryGetUploadedAsync(slot.StoragePath, default);
        Assert.NotNull(after);
        Assert.Equal(bytes.LongLength, after!.Length);
        Assert.Equal(req.ContentType, after.ContentType);
        Assert.False(string.IsNullOrWhiteSpace(after.ETag));
    }

    [Fact]
    public async Task OpenRead_AndDelete_Behavior()
    {
        var store = new FakeAttachmentStorage();
        var path = $"x/{Guid.NewGuid()}/a.txt";

        await Assert.ThrowsAsync<FileNotFoundException>(() => store.OpenReadAsync(path, default));

        var data = System.Text.Encoding.UTF8.GetBytes("hello");
        store.SimulateClientUpload(path, data, MediaTypeNames.Text.Plain);

        await using var stream = await store.OpenReadAsync(path, default);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        Assert.Equal(data, ms.ToArray());

        await store.DeleteAsync(path, default);
        var props = await store.TryGetUploadedAsync(path, default);
        Assert.Null(props);
        await Assert.ThrowsAsync<FileNotFoundException>(() => store.OpenReadAsync(path, default));
    }

    [Fact]
    public async Task SimulateClientUpload_ChangesETag_OnEachWrite()
    {
        var store = new FakeAttachmentStorage();
        var path = $"p/{Guid.NewGuid()}/b.bin";

        store.SimulateClientUpload(path, new byte[] { 0x01 }, "application/octet-stream");
        var p1 = await store.TryGetUploadedAsync(path, default);
        Assert.NotNull(p1);

        store.SimulateClientUpload(path, new byte[] { 0x02, 0x03 }, "application/octet-stream");
        var p2 = await store.TryGetUploadedAsync(path, default);
        Assert.NotNull(p2);

        Assert.NotEqual(p1!.ETag, p2!.ETag);
        Assert.True(p2.Length > p1.Length);
    }

    [Fact]
    public async Task TryGet_NotExistingPath_ReturnsNull()
    {
        var store = new FakeAttachmentStorage();
        var result = await store.TryGetUploadedAsync("never/created/path/file.bin", default);
        Assert.Null(result);
    }
}
