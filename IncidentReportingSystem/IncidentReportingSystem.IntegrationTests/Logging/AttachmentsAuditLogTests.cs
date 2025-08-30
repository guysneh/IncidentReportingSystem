using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Logging;

public sealed class AttachmentsAuditLogTests : IClassFixture<LoggingAttachmentsWebApplicationFactory>
{
    private readonly LoggingAttachmentsWebApplicationFactory _factory;

    public AttachmentsAuditLogTests(LoggingAttachmentsWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "Attachments.Complete emits audit log with EventId + tags and no PII")]
    public async Task Complete_Emits_Audit_Log()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        // Start
        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"t-{Guid.NewGuid():N}.png";
        var start = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }), Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();

        using var startDoc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        // Upload via loopback
        var put = new ByteArrayContent(Encoding.UTF8.GetBytes("png-bytes"));
        put.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var putRes = await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}", put);
        putRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Complete
        var complete = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: log captured
        var rec = _factory.Provider.Records
            .FirstOrDefault(r => r.EventId.Id == AuditEvents.Attachments.Complete.Id);
        rec.Should().NotBeNull("Complete should emit audit log");

        rec!.TryGetTags().Should().Be("attachments,complete");

        // No sensitive data in structured state
        rec.State.Should().NotContain(kv => kv.Key.Contains("Password", StringComparison.OrdinalIgnoreCase));
        rec.State.Should().NotContain(kv => kv.Key.Contains("StoragePath", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Attachments.Download emits audit log with EventId + tags")]
    public async Task Download_Emits_Audit_Log()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        // Create and upload quickly
        var incidentId = KnownIds.ExistingIncidentId(_factory);
        var fileName = $"d-{Guid.NewGuid():N}.png";
        var start = await client.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName, contentType = "image/png" }), Encoding.UTF8, "application/json"));
        start.EnsureSuccessStatusCode();

        using var startDoc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        var put = new ByteArrayContent(Encoding.UTF8.GetBytes("png-bytes"));
        put.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        (await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}", put))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        (await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent("")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Trigger download
        var download = await client.GetAsync($"{apiRoot}/attachments/{attachmentId}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: log captured
        var rec = _factory.Provider.Records
            .FirstOrDefault(r => r.EventId.Id == AuditEvents.Attachments.Download.Id);
        rec.Should().NotBeNull("Download should emit audit log");
        rec!.TryGetTags().Should().Be("attachments,download");
    }
}
