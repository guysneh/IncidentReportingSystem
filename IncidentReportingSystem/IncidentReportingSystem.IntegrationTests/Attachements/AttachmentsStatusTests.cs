using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments
{
    [Trait("Category", "Integration")]
    public sealed class AttachmentsStatusTests : IClassFixture<AttachmentsWebApplicationFactory>
    {
        private readonly AttachmentsWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AttachmentsStatusTests(AttachmentsWebApplicationFactory factory)
        {
            _factory = factory;
            _client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        }

        [Fact(DisplayName = "Status: Pending → Completed (Loopback PUT flow)")]
        public async Task Status_Pending_To_Completed_Works()
        {
            // Resolve API root robustly (handles base path + api versioning)
            var apiRoot = await ApiRootResolver.ResolveAsync(_factory, _client);

            // 1) Start upload for a known incident
            var incidentId = KnownIds.ExistingIncidentId(_factory);
            var fileName = $"photo-{Guid.NewGuid():N}.png";
            var startBody = new { fileName, contentType = "image/png" };

            var startRes = await _client.PostAsync(
                $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
                new StringContent(JsonSerializer.Serialize(startBody), Encoding.UTF8, "application/json"));

            startRes.StatusCode.Should().Be(HttpStatusCode.OK);
            using var startDoc = JsonDocument.Parse(await startRes.Content.ReadAsStringAsync());

            var attachmentId = startDoc.RootElement.GetProperty("attachmentId").GetGuid();
            var storagePath = startDoc.RootElement.GetProperty("storagePath").GetString()!;
            startDoc.RootElement.TryGetProperty("method", out var methodProp).Should().BeTrue();
            methodProp.GetString().Should().Be("PUT");

            // 2) Upload bytes via loopback PUT endpoint
            var fileBytes = Encoding.UTF8.GetBytes("fake-png-bytes");
            var putContent = new ByteArrayContent(fileBytes);
            putContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

            var uploadPutUrl = $"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}";
            var putRes = await _client.PutAsync(uploadPutUrl, putContent);
            putRes.StatusCode.Should().Be(HttpStatusCode.Created);

            // 3) Complete upload
            var completeRes = await _client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", content: null);
            completeRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // 4) Check status → Completed + existsInStorage + size>0 + contentType set
            var status = await _client.GetFromJsonAsync<AttachmentStatusDto>($"{apiRoot}/attachments/{attachmentId}/status");
            status.Should().NotBeNull();
            status!.Status.ToString().Should().Be("Completed");
            status.ExistsInStorage.Should().BeTrue();
            status.Size.Should().BeGreaterThan(0);
            status.ContentType.Should().NotBeNullOrWhiteSpace();
        }

        [Fact(DisplayName = "Status: 404 for unknown attachment")]
        public async Task Status_NotFound_For_Unknown_Attachment()
        {
            var apiRoot = await ApiRootResolver.ResolveAsync(_factory, _client);
            var resp = await _client.GetAsync($"{apiRoot}/attachments/{Guid.NewGuid()}/status");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
