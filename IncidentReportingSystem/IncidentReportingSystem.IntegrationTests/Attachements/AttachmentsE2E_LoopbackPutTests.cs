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
}
