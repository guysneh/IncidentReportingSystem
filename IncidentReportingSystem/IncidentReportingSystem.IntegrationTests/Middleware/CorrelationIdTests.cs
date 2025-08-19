using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using static IncidentReportingSystem.IntegrationTests.Utils.CustomWebApplicationFactory;

namespace IncidentReportingSystem.Tests.Integration.Middleware;

public class CorrelationIdTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public CorrelationIdTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Generate_CorrelationId_If_Missing()
    {
        using var client = _factory.AsUser();
        var res = await client.GetAsync(RouteHelper.R(_factory, "incidentreports"));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.Contains("X-Correlation-ID").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Respect_Existing_CorrelationId()
    {
        using var client = _factory.AsUser();
        var corr = Guid.NewGuid().ToString();

        var req = new HttpRequestMessage(HttpMethod.Get, RouteHelper.R(_factory, "incidentreports"));
        req.Headers.Add("X-Correlation-ID", corr);

        var res = await client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.GetValues("X-Correlation-ID").First().Should().Be(corr);
    }
}
