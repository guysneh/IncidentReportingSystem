using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.Tests.Integration.Middleware;

public class CorrelationIdTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorrelationIdTests(CustomWebApplicationFactory factory)
    {
        _client = AuthenticatedHttpClientFactory.CreateClientWithToken(factory);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Generate_CorrelationId_If_Missing()
    {
        var response = await _client.GetAsync("/api/v1/incidentreports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Correlation-ID");
        response.Headers.GetValues("X-Correlation-ID").First().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Respect_Existing_CorrelationId()
    {
        var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Correlation-ID").First().Should().Be(correlationId);
    }
}
