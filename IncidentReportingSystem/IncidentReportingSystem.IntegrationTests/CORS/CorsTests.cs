using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.Tests.Integration.CORS;

public class CORSTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CORSTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Allow_Configured_Origin_For_Get_Request()
    {
        using var client = _factory.AsUser();
        client.DefaultRequestHeaders.Add("Origin", "http://example.com");

        var res = await client.GetAsync($"/api/{TestConstants.ApiVersion}/incidentreports");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.TryGetValues("Access-Control-Allow-Origin", out var ao).Should().BeTrue();
        ao!.First().Should().Be("http://example.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Respond_To_Preflight_Request_For_Configured_Origin()
    {
        using var client = _factory.AsUser();
        var req = new HttpRequestMessage(HttpMethod.Options, "/api/{TestConstants.ApiVersion}/incidentreports");
        req.Headers.Add("Origin", "http://example.com");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

