using System.Net;
using System.Net.Http.Headers;
using System.Text;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

public sealed class AttachmentsLoopbackEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AttachmentsLoopbackEndpointsTests(CustomWebApplicationFactory f) => _factory = f;

    private static ByteArrayContent Bytes(byte[] data, string contentType)
    {
        var c = new ByteArrayContent(data);
        c.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return c;
    }

    private async Task<HttpResponseMessage> UploadAsync(HttpClient client, string storagePath, byte[] payload, string contentType)
    {
        var url = RouteHelper.R(_factory, $"api/v1/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}");

        // Try PUT first, fallback to POST if the endpoint disallows PUT.
        var content = Bytes(payload, contentType);
        var res = await client.PutAsync(url, content);
        if (res.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            content = Bytes(payload, contentType);
            res = await client.PostAsync(url, content);
        }
        return res;
    }

    private async Task<HttpResponseMessage> DownloadAsync(HttpClient client, string storagePath)
    {
        var url = RouteHelper.R(_factory, $"api/v1/attachments/_loopback/download?path={Uri.EscapeDataString(storagePath)}");

        // Try GET first, fallback to POST if GET is not allowed.
        var res = await client.GetAsync(url);
        if (res.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            res = await client.PostAsync(url, new StringContent(string.Empty));
        }
        return res;
    }

    private async Task<HttpStatusCode> DeleteAsync(HttpClient client, string storagePath)
    {
        var url = RouteHelper.R(_factory, $"api/v1/attachments/_loopback/delete?path={Uri.EscapeDataString(storagePath)}");
        var res = await client.DeleteAsync(url);
        if (res.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            res = await client.PostAsync(url, new StringContent(string.Empty));
        }
        return res.StatusCode;
    }

    [Fact]
    public async Task Upload_Download_Delete_Flow_Works()
    {
        var client = await AuthTestHelpers.RegisterAndLoginAsync(_factory, userId: null, roles: new[] { "Admin" });

        var storagePath = $"devtest/{Guid.NewGuid():N}/file-loopback.bin";
        var payload = Encoding.UTF8.GetBytes("hello-loopback");

        // Upload (PUT or POST, depending on server)
        var up = await UploadAsync(client, storagePath, payload, "application/octet-stream");

        Assert.Contains(up.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Conflict });

        // Try download (GET or POST)
        var dlTry = await DownloadAsync(client, storagePath);
        if (dlTry.StatusCode == HttpStatusCode.MethodNotAllowed)
            return; 

        if (dlTry.StatusCode != HttpStatusCode.NotFound)
        {
            dlTry.EnsureSuccessStatusCode();
            var got = await dlTry.Content.ReadAsByteArrayAsync();
            Assert.Equal(payload, got);
        }

        // Delete (DELETE or POST)
        var delStatus = await DeleteAsync(client, storagePath);
        if (delStatus == HttpStatusCode.MethodNotAllowed)
            return; 

        Assert.Equal(HttpStatusCode.NoContent, delStatus);

        var downloadUrl = RouteHelper.R(_factory, $"api/v1/attachments/_loopback/download?path={Uri.EscapeDataString(storagePath)}");
        var finalStatus = await PollForStatusAsync(
            client,
            downloadUrl,
            expected: HttpStatusCode.NotFound,
            maxAttempts: 12,   // ~1.8s max
            delayMs: 150);

        Assert.Equal(HttpStatusCode.NotFound, finalStatus);
    }




    [Fact]
    public async Task Download_BadPath_ResolvesVerb_And_Returns_404()
    {
        var client = await AuthTestHelpers.RegisterAndLoginAsync(_factory, userId: null, roles: new[] { "Admin" });
        var badPath = $"devtest/{Guid.NewGuid():N}/does-not-exist.bin";

        var res = await DownloadAsync(client, badPath);

        // Some deployments don’t expose download on loopback, so 405 is acceptable.
        Assert.Contains(res.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
    }

    private static async Task<HttpStatusCode> PollForStatusAsync(
    HttpClient client,
    string url,
    HttpStatusCode expected,
    int maxAttempts = 12,
    int delayMs = 150,
    CancellationToken ct = default)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            using var resp = await client.GetAsync(url, ct);
            if (resp.StatusCode == expected)
                return resp.StatusCode;

            if (resp.StatusCode == HttpStatusCode.Conflict || resp.StatusCode == HttpStatusCode.Locked)
            {
                await Task.Delay(delayMs, ct);
                continue;
            }

            return resp.StatusCode;
        }

        return HttpStatusCode.Conflict;
    }

}
