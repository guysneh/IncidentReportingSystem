using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsSignedUrlTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    public AttachmentsSignedUrlTests(AttachmentsWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "Signed download URL allows anonymous download before expiry")]
    public async Task SignedUrl_Allows_Anonymous_Download()
    {
        var authed = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, authed);

        // Start
        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"img-{Guid.NewGuid():N}.png";
        var startRes = await authed.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        startRes.EnsureSuccessStatusCode();

        using var startDoc = JsonDocument.Parse(await startRes.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        // PUT upload (loopback)
        var bytes = Encoding.UTF8.GetBytes("signed-url-png");
        var put = new ByteArrayContent(bytes);
        put.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var uploadUrl = $"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}";
        var putRes = await authed.PutAsync(uploadUrl, put);
        putRes.StatusCode.Should().Be(HttpStatusCode.Created, await putRes.Content.ReadAsStringAsync());

        // Complete
        var complete = await authed.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Request signed URL
        var signed = await authed.PostAsync($"{apiRoot}/attachments/{attachmentId}/download-url?ttlMinutes=5", new StringContent(""));
        signed.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await signed.Content.ReadAsStringAsync());
        var url = doc.RootElement.GetProperty("url").GetString()!;
        url.Should().NotBeNullOrWhiteSpace();

        // Anonymous download (no bearer)
        var anon = _factory.CreateClient();
        var dl = await anon.GetAsync(url);
        dl.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await dl.Content.ReadAsByteArrayAsync();
        payload.Should().BeEquivalentTo(bytes);
        dl.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }
}
