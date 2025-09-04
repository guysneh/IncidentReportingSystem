using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsDownloadETagTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AttachmentsDownloadETagTests(AttachmentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact(DisplayName = "Download returns ETag and supports If-None-Match → 304")]
    public async Task Download_With_ETag_Conditional_304()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        // 1) Start
        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"photo-{Guid.NewGuid():N}.png";
        var startRes = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        startRes.EnsureSuccessStatusCode();

        using var startDoc = JsonDocument.Parse(await startRes.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        // 2) Upload via loopback
        var bytes = Encoding.UTF8.GetBytes("fake-png-bytes");
        var put = new ByteArrayContent(bytes);
        put.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var uploadPutUrl = $"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}";
        var putRes = await client.PutAsync(uploadPutUrl, put);
        putRes.StatusCode.Should().Be(HttpStatusCode.Created, await putRes.Content.ReadAsStringAsync());

        // 3) Complete
        var completeRes = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        completeRes.StatusCode.Should().Be(HttpStatusCode.NoContent, await completeRes.Content.ReadAsStringAsync());

        // 4) Download (200 + ETag + Cache-Control)
        var download1 = await client.GetAsync($"{apiRoot}/attachments/{attachmentId}/download");
        download1.StatusCode.Should().Be(HttpStatusCode.OK, await download1.Content.ReadAsStringAsync());

        var etag = download1.Headers.ETag?.Tag;
        etag.Should().NotBeNullOrWhiteSpace("download should return an ETag");
        _output.WriteLine($"ETag = {etag}");

        // Cache-Control should be private with a positive max-age
        var cc1 = download1.Headers.CacheControl;
        cc1.Should().NotBeNull();
        cc1!.Private.Should().BeTrue();
        cc1.MaxAge.Should().NotBeNull();
        cc1.MaxAge!.Value.TotalSeconds.Should().BeGreaterThan(0);

        var body1 = await download1.Content.ReadAsByteArrayAsync();
        body1.Should().BeEquivalentTo(bytes);
        download1.Content.Headers.ContentType!.MediaType.Should().Be("image/png");

        // 5) Conditional GET with If-None-Match → 304
        var req2 = new HttpRequestMessage(HttpMethod.Get, $"{apiRoot}/attachments/{attachmentId}/download");
        req2.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(etag!));
        var download2 = await client.SendAsync(req2);
        download2.StatusCode.Should().Be(HttpStatusCode.NotModified);

        // Should echo the same ETag and not send a body
        download2.Headers.ETag?.Tag.Should().Be(etag);
        var body2 = await download2.Content.ReadAsByteArrayAsync();
        body2.Length.Should().Be(0);

        // 6) If-None-Match with mismatched ETag → 200
        var req3 = new HttpRequestMessage(HttpMethod.Get, $"{apiRoot}/attachments/{attachmentId}/download");
        req3.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue("\"bogus-etag\""));
        var download3 = await client.SendAsync(req3);
        download3.StatusCode.Should().Be(HttpStatusCode.OK);
        download3.Headers.ETag?.Tag.Should().Be(etag);
    }
}
