using IncidentReportingSystem.IntegrationTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachements
{
    [Trait("TestType", "Integration")]
    public sealed class AttachmentsNegativeFlowsTests : IClassFixture<AttachmentsWebApplicationFactory>
    {
        private readonly AttachmentsWebApplicationFactory _f;
        private readonly ITestOutputHelper _output;
        public AttachmentsNegativeFlowsTests(AttachmentsWebApplicationFactory f, ITestOutputHelper o) { _f = f; _output = o; }

        [Fact(DisplayName = "Complete before upload → 409")]
        public async Task Complete_Before_Upload_409()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
            var apiRoot = await ApiRootResolver.ResolveAsync(_f, client);
            var incidentId = KnownIds.ExistingIncidentId(_f);

            var start = await client.PostAsync($"{apiRoot}/incidentreports/{incidentId}/attachments/start",
                new StringContent(JsonSerializer.Serialize(new { fileName = $"pre-{Guid.NewGuid():N}.png", contentType = "image/png" }), Encoding.UTF8, "application/json"));
            await start.ShouldBeAsync(HttpStatusCode.OK, _output, "Start");

            using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
            var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();

            var complete = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
            await complete.ShouldBeAsync(HttpStatusCode.Conflict, _output, "Complete before upload");
        }

        [Fact(DisplayName = "Complete twice → 409 on second attempt")]
        public async Task Complete_Twice_409()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
            var apiRoot = await ApiRootResolver.ResolveAsync(_f, client);
            var incidentId = KnownIds.ExistingIncidentId(_f);

            var start = await client.PostAsync($"{apiRoot}/incidentreports/{incidentId}/attachments/start",
                new StringContent(JsonSerializer.Serialize(new { fileName = $"twice-{Guid.NewGuid():N}.jpg", contentType = "image/jpeg" }), Encoding.UTF8, "application/json"));
            await start.ShouldBeAsync(HttpStatusCode.OK, _output, "Start");

            using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
            var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();
            var storagePath = doc.RootElement.GetProperty("storagePath").GetString()!;
            var upload = await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}", new ByteArrayContent(Encoding.UTF8.GetBytes("x")));
            await upload.ShouldBeAsync(HttpStatusCode.Created, _output, "Upload");
            var c1 = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
            await c1.ShouldBeAsync(HttpStatusCode.NoContent, _output, "Complete #1");
            var c2 = await client.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
            await c2.ShouldBeAsync(HttpStatusCode.Conflict, _output, "Complete #2");
        }

        [Fact(DisplayName = "Upload with invalid prefix → 409")]
        public async Task Upload_InvalidPrefix_409()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
            var apiRoot = await ApiRootResolver.ResolveAsync(_f, client);
            var invalidPath = "invalid_prefix/foo/bar.jpg";
            var res = await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path={Uri.EscapeDataString(invalidPath)}", new ByteArrayContent(Encoding.UTF8.GetBytes("x")));
            await res.ShouldBeAsync(HttpStatusCode.Conflict, _output, "Invalid prefix");
        }
    }
}
