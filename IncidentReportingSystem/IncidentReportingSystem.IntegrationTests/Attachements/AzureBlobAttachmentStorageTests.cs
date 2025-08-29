using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.Storage;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

public sealed class AzureBlobAttachmentStorageTests
{
    private readonly ITestOutputHelper _output;
    public AzureBlobAttachmentStorageTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Ctor_Throws_OnInvalidOptions()
    {
        Assert.Throws<ArgumentException>(() => new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: "", AccountName: "acc", AccountKey: "key", Container: "c", UploadSasTtl: TimeSpan.FromMinutes(5), PublicEndpoint: null)));

        Assert.Throws<ArgumentException>(() => new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: "http://x", AccountName: "", AccountKey: "key", Container: "c", UploadSasTtl: TimeSpan.FromMinutes(5), PublicEndpoint: null)));

        Assert.Throws<ArgumentException>(() => new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: "http://x", AccountName: "acc", AccountKey: "key", Container: "", UploadSasTtl: TimeSpan.FromMinutes(5), PublicEndpoint: null)));

        Assert.Throws<ArgumentException>(() => new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: "http://x", AccountName: "acc", AccountKey: "key", Container: "c", UploadSasTtl: TimeSpan.Zero, PublicEndpoint: null)));
    }

    [Fact]
    public async Task CreateUploadSlot_ArgumentValidation()
    {
        var storage = new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: "http://127.0.0.1:10000/devstoreaccount1",
            AccountName: "devstoreaccount1",
            AccountKey: "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890",
            Container: "test",
            UploadSasTtl: TimeSpan.FromMinutes(15),
            PublicEndpoint: null));

        await Assert.ThrowsAsync<ArgumentNullException>(() => storage.CreateUploadSlotAsync(null!, default));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.CreateUploadSlotAsync(new CreateUploadSlotRequest(Guid.NewGuid(), "", "text/plain", "p"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.CreateUploadSlotAsync(new CreateUploadSlotRequest(Guid.NewGuid(), "a.txt", "", "p"), default));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.CreateUploadSlotAsync(new CreateUploadSlotRequest(Guid.NewGuid(), "a.txt", "text/plain", ""), default));
    }

    [Fact]
    public async Task TryGet_OpenRead_Delete_ArgumentValidation()
    {
        var storage = new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: "http://127.0.0.1:10000/devstoreaccount1",
            AccountName: "devstoreaccount1",
            AccountKey: "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890",
            Container: "test",
            UploadSasTtl: TimeSpan.FromMinutes(1),
            PublicEndpoint: null));

        await Assert.ThrowsAsync<ArgumentException>(() => storage.TryGetUploadedAsync("", default));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenReadAsync("   ", default));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync("   ", default));
    }

    /// <summary>
    /// Optional integration against Azurite/real Blob. Enabled only when env vars are provided:
    ///  AZURE_BLOB_ENDPOINT   (e.g. http://127.0.0.1:10000/devstoreaccount1)
    ///  AZURE_BLOB_ACCOUNT    (e.g. devstoreaccount1)
    ///  AZURE_BLOB_KEY        (Azurite account key)
    ///  AZURE_BLOB_CONTAINER  (e.g. attachments)
    /// </summary>
    [Fact]
    public async Task Integration_With_Azurite_IfAvailable()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_BLOB_ENDPOINT");
        var account = Environment.GetEnvironmentVariable("AZURE_BLOB_ACCOUNT");
        var key = Environment.GetEnvironmentVariable("AZURE_BLOB_KEY");
        var container = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER");

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(account) ||
            string.IsNullOrWhiteSpace(key) ||
            string.IsNullOrWhiteSpace(container))
        {
            _output.WriteLine("Skipping Azurite integration (missing env vars).");
            return;
        }

        var storage = new AzureBlobAttachmentStorage(new AzureBlobAttachmentStorage.Options(
            Endpoint: endpoint!,
            AccountName: account!,
            AccountKey: key!,
            Container: container!,
            UploadSasTtl: TimeSpan.FromMinutes(5),
            PublicEndpoint: null));

        var req = new CreateUploadSlotRequest(
            AttachmentId: Guid.NewGuid(),
            FileName: "r e p o r t(1).pdf",
            ContentType: MediaTypeNames.Application.Pdf,
            PathPrefix: "incidents/2025/08");

        var slot = await storage.CreateUploadSlotAsync(req, default);
        Assert.True(slot.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Contains($"/{container}/", slot.UploadUrl.AbsoluteUri);

        using var http = new HttpClient();
        var payload = new ByteArrayContent(Encoding.UTF8.GetBytes("hello blob"));
        payload.Headers.Add("x-ms-blob-type", "BlockBlob");
        payload.Headers.ContentType = new MediaTypeHeaderValue(req.ContentType);

        var put = await http.PutAsync(slot.UploadUrl, payload);
        Assert.True(put.IsSuccessStatusCode, $"Upload failed: {(int)put.StatusCode} {put.ReasonPhrase}");

        var props = await storage.TryGetUploadedAsync(slot.StoragePath, default);
        Assert.NotNull(props);
        Assert.Equal("application/pdf", props!.ContentType);
        Assert.True(props.Length > 0);
        Assert.False(string.IsNullOrWhiteSpace(props.ETag));

        await using var s = await storage.OpenReadAsync(slot.StoragePath, default);
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms);
        Assert.Equal("hello blob", Encoding.UTF8.GetString(ms.ToArray()));

        await storage.DeleteAsync(slot.StoragePath, default);
        var afterDel = await storage.TryGetUploadedAsync(slot.StoragePath, default);
        Assert.Null(afterDel);
    }
}
