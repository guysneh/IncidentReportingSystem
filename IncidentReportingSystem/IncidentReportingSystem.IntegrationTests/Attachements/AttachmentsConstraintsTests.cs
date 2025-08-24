using System.Net;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsConstraintsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AttachmentsConstraintsTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact(DisplayName = "GET /attachments/constraints returns 200 and allowed content types")]
    public async Task Constraints_Returns_200()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);
        var url = $"{apiRoot}/attachments/constraints";
        _output.WriteLine($"Resolved ApiRoot={apiRoot}  ConstraintsUrl={url}");

        var res = await client.GetAsync(url);
        await res.ShouldBeAsync(HttpStatusCode.OK, _output, "constraints endpoint");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("allowedContentTypes", out var types).Should().BeTrue();
        types.EnumerateArray().Should().NotBeEmpty();
    }
}
