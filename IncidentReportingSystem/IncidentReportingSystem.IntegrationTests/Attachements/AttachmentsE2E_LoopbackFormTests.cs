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
public sealed class AttachmentsE2E_LoopbackFormTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AttachmentsE2E_LoopbackFormTests(AttachmentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact(DisplayName = "Start → upload-form (multipart) → Complete → Download")]
    public async Task Full_E2E_Form_Works()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);
        _output.WriteLine($"Resolved ApiRoot = {apiRoot}");

        // 1) Start
        var incidentId = KnownIds.ExistingIncidentId(_factory); 
        var startUrl = $"{apiRoot}/incidentreports/{incidentId}/attachments/start";


        var fileName = $"guy-{Guid.NewGuid():N}.jpg";
        var startBody = JsonSerializer.Serialize(new { fileName, contentType = "image/jpeg" });
        var startRes = await client.PostAsync(startUrl, new StringContent(startBody, Encoding.UTF8, "application/json"));
        await startRes.ShouldBeAsync(HttpStatusCode.OK, _output, "Start");

        using var startDoc = JsonDocument.Parse(await startRes.Content.ReadAsStringAsync());
        var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
        var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;

        // 2) Upload (multipart form)
        var uploadUrl = $"{apiRoot}/attachments/_loopback/upload-form";
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(storagePath), "path");
        var fileBytes = Encoding.UTF8.GetBytes("fake-jpg-content");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", fileName);

        var uploadRes = await client.PostAsync(uploadUrl, content);
        await uploadRes.ShouldBeAsync(HttpStatusCode.Created, _output, "Upload form");

        // 3) Complete
        var completeUrl = $"{apiRoot}/attachments/{attachmentId}/complete";
        var completeRes = await client.PostAsync(completeUrl, new StringContent(""));
        await completeRes.ShouldBeAsync(HttpStatusCode.NoContent, _output, "Complete");

        // 4) Download
        var downloadUrl = $"{apiRoot}/attachments/{attachmentId}/download";
        var downloadRes = await client.GetAsync(downloadUrl);
        await downloadRes.ShouldBeAsync(HttpStatusCode.OK, _output, "Download");

        var bytes = await downloadRes.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().Be(fileBytes.Length);
        downloadRes.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }
}
