using System.Net;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsConstraintsEndpointTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _f;
    private readonly ITestOutputHelper _o;

    public AttachmentsConstraintsEndpointTests(AttachmentsWebApplicationFactory f, ITestOutputHelper o)
    {
        _f = f; _o = o;
    }

    [Fact(DisplayName = "GET /attachments/constraints returns 200 and reflects AttachmentOptions")]
    public async Task Constraints_Returns_Options()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
        var root = await ApiRootResolver.ResolveAsync(_f, client);

        var res = await client.GetAsync($"{root}/attachments/constraints");
        await res.ShouldBeAsync(HttpStatusCode.OK, _o, "constraints endpoint should be public and stable");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var ro = doc.RootElement;

        ro.TryGetProperty("allowedContentTypes", out var types).Should().BeTrue();
        types.EnumerateArray().Select(e => e.GetString()).Should().BeEquivalentTo(
            new[] { "image/jpeg", "image/png", "application/pdf" });

        ro.TryGetProperty("allowedExtensions", out var exts).Should().BeTrue();
        exts.EnumerateArray().Select(e => e.GetString()).Should().Contain(new[] { ".jpg", ".jpeg", ".png", ".pdf" });

        ro.TryGetProperty("maxSizeBytes", out var max).Should().BeTrue();
        max.GetInt64().Should().BeGreaterThan(0);

        ro.TryGetProperty("uploadUrlTtlMinutes", out var ttl).Should().BeTrue();
        ttl.GetInt32().Should().BeGreaterThan(0);
    }
}
