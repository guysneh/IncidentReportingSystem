using System.Net;
using System.Text;
using System.Text.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsDuplicateUploadTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _f;
    private readonly ITestOutputHelper _o;
    public AttachmentsDuplicateUploadTests(AttachmentsWebApplicationFactory f, ITestOutputHelper o) { _f = f; _o = o; }

    [Fact(DisplayName = "Uploading same storagePath twice returns 409")]
    public async Task Duplicate_Upload_409()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
        var root = await ApiRootResolver.ResolveAsync(_f, client);
        var incidentId = KnownIds.ExistingIncidentId(_f);

        var start = await client.PostAsync($"{root}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName = $"dup-{Guid.NewGuid():N}.png", contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        await start.ShouldBeAsync(HttpStatusCode.OK, _o, "start");

        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var path = doc.RootElement.GetProperty("storagePath").GetString()!;

        var u1 = await client.PutAsync($"{root}/attachments/_loopback/upload?path={Uri.EscapeDataString(path)}",
            new ByteArrayContent(Encoding.UTF8.GetBytes("x")));
        await u1.ShouldBeAsync(HttpStatusCode.Created, _o, "first upload");

        var u2 = await client.PutAsync($"{root}/attachments/_loopback/upload?path={Uri.EscapeDataString(path)}",
            new ByteArrayContent(Encoding.UTF8.GetBytes("y")));
        await u2.ShouldBeAsync(HttpStatusCode.Conflict, _o, "duplicate upload must 409");
    }
}
