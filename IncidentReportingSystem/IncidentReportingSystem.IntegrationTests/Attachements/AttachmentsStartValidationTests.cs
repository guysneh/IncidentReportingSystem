using System.Net;
using System.Text;
using System.Text.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("TestType", "Integration")]
public sealed class AttachmentsStartValidationTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _f;
    private readonly ITestOutputHelper _o;
    public AttachmentsStartValidationTests(AttachmentsWebApplicationFactory f, ITestOutputHelper o) { _f = f; _o = o; }

    [Fact(DisplayName = "Start with disallowed content-type → 400")]
    public async Task Start_Disallowed_ContentType_400()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
        var root = await ApiRootResolver.ResolveAsync(_f, client);
        var incidentId = KnownIds.ExistingIncidentId(_f);

        var payload = new { fileName = $"bad-{Guid.NewGuid():N}.zip", contentType = "application/zip" };
        var res = await client.PostAsync(
            $"{root}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        await res.ShouldBeAsync(HttpStatusCode.BadRequest, _o, "disallowed content-type must be rejected");
    }

    [Fact(DisplayName = "Start with mismatched extension → 400")]
    public async Task Start_Mismatched_Extension_400()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
        var root = await ApiRootResolver.ResolveAsync(_f, client);
        var incidentId = KnownIds.ExistingIncidentId(_f);

        var res = await client.PostAsync($"{root}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName = $"evil-{Guid.NewGuid():N}.exe", contentType = "image/jpeg" }),
            Encoding.UTF8, "application/json"));

        await res.ShouldBeAsync(HttpStatusCode.BadRequest, _o, "mismatched extension must 400");
    }

    [Trait("Category", "Integration")]
    public sealed class AttachmentsStartMoreValidationTests : IClassFixture<AttachmentsWebApplicationFactory>
    {
        private readonly AttachmentsWebApplicationFactory _f;
        private readonly ITestOutputHelper _o;
        public AttachmentsStartMoreValidationTests(AttachmentsWebApplicationFactory f, ITestOutputHelper o)
        { _f = f; _o = o; }

        [Fact(DisplayName = "Start with disallowed content-type → 400")]
        public async Task Start_Disallowed_ContentType_400()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
            var root = await ApiRootResolver.ResolveAsync(_f, client);
            var incidentId = KnownIds.ExistingIncidentId(_f);

            var body = JsonSerializer.Serialize(new { fileName = $"img-{Guid.NewGuid():N}.jpg", contentType = "image/gif" });
            var res = await client.PostAsync($"{root}/incidentreports/{incidentId}/attachments/start",
                new StringContent(body, Encoding.UTF8, "application/json"));

            await res.ShouldBeAsync(HttpStatusCode.BadRequest, _o, "gif should be rejected by policy");
        }
    }
}
