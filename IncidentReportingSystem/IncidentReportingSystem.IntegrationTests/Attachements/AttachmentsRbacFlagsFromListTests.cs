using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.Domain;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsRbacFlagsFromListTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;

    public AttachmentsRbacFlagsFromListTests(AttachmentsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "List includes RBAC flags for completed attachment (canDownload=true, canDelete=false)")]
    public async Task List_Includes_Rbac_Flags_For_Completed_Attachment()
    {
        // Same client and flow as in passing E2E tests
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { Roles.Admin});
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        // 1) Start (identical pattern)
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

        // 2) Upload via loopback (PUT + ?path=... exactly like the passing tests)
        var fileBytes = Encoding.UTF8.GetBytes("fake-png-bytes");
        using var put = new ByteArrayContent(fileBytes);
        put.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var uploadPutUrl = $"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}";
        var putRes = await client.PutAsync(uploadPutUrl, put);
        putRes.StatusCode.Should().Be(HttpStatusCode.Created, await putRes.Content.ReadAsStringAsync());

        // 3) Complete (same as passing tests)
        var completeRes = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        completeRes.StatusCode.Should().Be(HttpStatusCode.NoContent, await completeRes.Content.ReadAsStringAsync());

        // 4) List for the incident and locate our item
        var listRes = await client.GetAsync($"{apiRoot}/incidentreports/{incidentId}/attachments?take=50");
        listRes.EnsureSuccessStatusCode();

        using var listDoc = JsonDocument.Parse(await listRes.Content.ReadAsStringAsync());
        // Support both paged { items: [...] } and raw array [ ... ]
        var itemsEl = listDoc.RootElement.TryGetProperty("items", out var itemsProp) ? itemsProp : listDoc.RootElement;

        JsonElement? found = null;
        foreach (var el in itemsEl.EnumerateArray())
        {
            if (el.GetProperty("id").GetGuid() == attachmentId)
            {
                found = el;
                break;
            }
        }

        found.Should().NotBeNull("the completed attachment should be present in the list");
        var item = found!.Value;

        // Assert flags + status
        item.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetString().Should().Be("Completed");

        item.TryGetProperty("canDownload", out var canDownload).Should().BeTrue();
        item.TryGetProperty("canDelete", out var canDelete).Should().BeTrue();

        canDownload.ValueKind.Should().Be(JsonValueKind.True);
        canDelete.ValueKind.Should().Be(JsonValueKind.False);
    }
}
