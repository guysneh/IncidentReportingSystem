using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.Features.Attachments;

public class LoopbackAttachmentStorageSecurityTests
{
    private static IConfiguration BuildConfig(string rootFolder) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Attachments:Storage"] = "Loopback",
                ["Attachments:Container"] = "attachments",
                ["Attachments:Loopback:Root"] = rootFolder.Replace('\\', '/'),
                ["Api:BasePath"] = "/api",
                ["Api:PublicBaseUrl"] = "https://localhost"
            })
            .Build();

    [Fact(DisplayName = "CreateUploadSlot returns URL targeting loopback endpoint and echoing storage path")]
    public async Task CreateSlot_ReturnsLoopbackUrl_AndStoragePath()
    {
        using var tmp = new TempDir();
        var cfg = BuildConfig(tmp.Path);
        var storage = new LoopbackAttachmentStorage(cfg);

        var req = new CreateUploadSlotRequest(
            AttachmentId: Guid.NewGuid(),
            FileName: "photo.png",
            ContentType: "image/png",
            PathPrefix: "incidents/" + Guid.NewGuid().ToString("D") + "/" + Guid.NewGuid().ToString("D"));

        var res = await storage.CreateUploadSlotAsync(req, CancellationToken.None);

        Assert.StartsWith("incidents/", res.StoragePath);
        Assert.Contains("/attachments/_loopback/upload", res.UploadUrl.AbsolutePath);
        // the query path should carry the same storage path
        Assert.EndsWith(Uri.EscapeDataString(res.StoragePath), res.UploadUrl.Query);
        Assert.True(res.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact(DisplayName = "TryGetUploaded throws on path traversal attempts")]
    public async Task TryGetUploaded_Traversal_Throws()
    {
        using var tmp = new TempDir();
        var cfg = BuildConfig(tmp.Path);
        var storage = new LoopbackAttachmentStorage(cfg);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.TryGetUploadedAsync("incidents/../evil.txt", CancellationToken.None));
    }

    [Fact(DisplayName = "OpenRead throws FileNotFound when object is missing")]
    public async Task OpenRead_NotFound()
    {
        using var tmp = new TempDir();
        var cfg = BuildConfig(tmp.Path);
        var storage = new LoopbackAttachmentStorage(cfg);

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            storage.OpenReadAsync("incidents/does/not/exist.bin", CancellationToken.None));
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }
        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "loopback_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }
        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true); } catch { /* ignore */ }
        }
    }
}
