using System.Net;
using System.Text;
using System.Text.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsValidationAndConflictsTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AttachmentsValidationAndConflictsTests(AttachmentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact(DisplayName = "Start with disallowed contentType returns 400")]
    public async Task Start_Disallowed_ContentType_Returns_400()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var startUrl = $"{apiRoot}/incidentreports/{incidentId}/attachments/start";

        var res = await client.PostAsync(
            startUrl,
            new StringContent(JsonSerializer.Serialize(new { fileName = $"x-{Guid.NewGuid():N}.bin", contentType = "text/plain" }),
            Encoding.UTF8, "application/json"));

        await res.ShouldBeAsync(HttpStatusCode.BadRequest, _output, "Start disallowed CT");
    }

    [Fact(DisplayName = "Complete returns 409 when uploaded content-type differs from declared")]
    public async Task Complete_ContentType_Mismatch_Returns_409()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        // Start declares image/png
        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"file-{Guid.NewGuid():N}.jpg";
        var startRes = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        await startRes.ShouldBeAsync(HttpStatusCode.OK, _output, "Start");

        using var startDoc = JsonDocument.Parse(await startRes.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        // Upload jpeg
        var putBytes = System.Text.Encoding.UTF8.GetBytes("jpeg-bytes");
        var putRes = await client.PutAsync(
            $"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}",
            new ByteArrayContent(putBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } });

        await putRes.ShouldBeAsync(HttpStatusCode.Created, _output, "Upload PUT");

        // Complete should conflict (409)
        var completeRes = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        await completeRes.ShouldBeAsync(HttpStatusCode.Conflict, _output, "Complete mismatch");
    }
}
