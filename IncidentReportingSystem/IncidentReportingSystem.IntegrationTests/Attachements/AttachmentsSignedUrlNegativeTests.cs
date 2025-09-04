using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsSignedUrlNegativeTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    public AttachmentsSignedUrlNegativeTests(AttachmentsWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "Tampered signature returns 401 Unauthorized")]
    public async Task Tampered_Signature_Unauthorized()
    {
        var authed = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, authed);
        var incidentId = KnownIds.ExistingIncidentId(_factory);

        // Start + upload + complete (shortened)
        var start = await authed.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName = "f.png", contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();
        using var startDoc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var id = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var path = startDoc.RootElement.GetProperty("storagePath").GetString()!;
        var put = await authed.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(path)}",
            new ByteArrayContent(Encoding.UTF8.GetBytes("x")) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") } });
        put.EnsureSuccessStatusCode();
        (await authed.PostAsync($"{apiRoot}/attachments/{id}/complete", new StringContent(""))).EnsureSuccessStatusCode();

        // Signed URL
        var signed = await authed.PostAsync($"{apiRoot}/attachments/{id}/download-url?ttlMinutes=5", new StringContent(""));
        using var doc = JsonDocument.Parse(await signed.Content.ReadAsStringAsync());
        var url = doc.RootElement.GetProperty("url").GetString()!;

        // Tamper last char in sig
        var tampered = url.Replace("sig=", "sig=a", StringComparison.Ordinal); // keep structure but change value

        var anon = _factory.CreateClient();
        var res = await anon.GetAsync(tampered);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
