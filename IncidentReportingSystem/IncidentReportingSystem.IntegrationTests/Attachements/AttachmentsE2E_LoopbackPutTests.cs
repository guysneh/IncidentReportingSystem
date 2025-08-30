using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsE2E_LoopbackPutTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AttachmentsE2E_LoopbackPutTests(AttachmentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact(DisplayName = "Start → PUT upload (binary) → Complete → Download")]
    public async Task Full_E2E_Put_Works()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);
        _output.WriteLine($"Resolved ApiRoot = {apiRoot}");

        // Start
        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"photo-{Guid.NewGuid():N}.png";
        var startRes = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        await startRes.ShouldBeAsync(HttpStatusCode.OK, _output, "Start");

        using var startDoc = JsonDocument.Parse(await startRes.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        startDoc.RootElement.TryGetProperty("method", out var methodProp).Should().BeTrue();
        methodProp.GetString().Should().Be("PUT");

        startDoc.RootElement.TryGetProperty("headers", out var headersProp).Should().BeTrue();
        headersProp.ValueKind.Should().Be(JsonValueKind.Object); // may be empty on loopback

        // Upload (PUT)
        var fileBytes = Encoding.UTF8.GetBytes("fake-png-bytes");
        var putContent = new ByteArrayContent(fileBytes);
        putContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var uploadPutUrl = $"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}";
        var putRes = await client.PutAsync(uploadPutUrl, putContent);
        await putRes.ShouldBeAsync(HttpStatusCode.Created, _output, "Upload PUT");

        // Complete
        var completeRes = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        await completeRes.ShouldBeAsync(HttpStatusCode.NoContent, _output, "Complete");

        // Download
        var downloadRes = await client.GetAsync($"{apiRoot}/attachments/{attachmentId}/download");
        await downloadRes.ShouldBeAsync(HttpStatusCode.OK, _output, "Download");

        var bytes = await downloadRes.Content.ReadAsByteArrayAsync();
        bytes.Should().BeEquivalentTo(fileBytes);
        downloadRes.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Fact(DisplayName = "Abort pending (no upload) → 204 and Complete → 404")]
    public async Task Abort_Pending_NoUpload_Then_Complete_Fails()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"a-{Guid.NewGuid():N}.png";

        var start = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }), Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();

        var abort = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/abort", new StringContent(""));
        abort.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var complete = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        complete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Abort pending with uploaded bytes → deletes blob & 204")]
    public async Task Abort_Pending_With_Uploaded_Bytes_Deletes_And_204()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"b-{Guid.NewGuid():N}.png";

        var start = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }), Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = doc.RootElement.GetProperty("storagePath").GetString()!;

        // upload via loopback
        var bytes = Encoding.UTF8.GetBytes("payload");
        var put = new ByteArrayContent(bytes);
        put.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var putRes = await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}", put);
        putRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // abort
        var abort = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/abort", new StringContent(""));
        abort.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // complete now should be 404
        var complete = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        complete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Abort completed → 409 Conflict")]
    public async Task Abort_Completed_409()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"c-{Guid.NewGuid():N}.png";

        var start = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }), Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = doc.RootElement.GetProperty("storagePath").GetString()!;

        // upload + complete
        var put = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
        put.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var putRes = await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}", put);
        putRes.EnsureSuccessStatusCode();
        var complete = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // abort should be 409
        var abort = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/abort", new StringContent(""));
        abort.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Abort by stranger → 403; by Admin → 204")]
    public async Task Abort_Authorization_Owner_Or_Admin()
    {
        // owner
        var owner = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { "User" });
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, owner);
        var incidentId = KnownIds.ExistingIncidentId(_factory);

        var start = await owner.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName = $"d-{Guid.NewGuid():N}.png", contentType = "image/png" }), Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();

        // stranger (not admin)
        var stranger = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { "User" });
        var abortStranger = await stranger.PostAsync($"{apiRoot}/attachments/{attachmentId}/abort", new StringContent(""));
        abortStranger.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // admin
        var admin = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { "Admin" });
        var abortAdmin = await admin.PostAsync($"{apiRoot}/attachments/{attachmentId}/abort", new StringContent(""));
        abortAdmin.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "Abort unknown id → 404 Not Found")]
    public async Task Abort_NotFound_404()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var abort = await client.PostAsync($"{apiRoot}/attachments/{Guid.NewGuid()}/abort", new StringContent(""));
        abort.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
