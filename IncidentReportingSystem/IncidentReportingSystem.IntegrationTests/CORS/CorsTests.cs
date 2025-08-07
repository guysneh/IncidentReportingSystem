using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.Tests.Integration.CORS;

public class CORSTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CORSTests(CustomWebApplicationFactory factory)
    {
        _client = AuthenticatedHttpClientFactory.CreateClientWithTokenAsync(factory).GetAwaiter().GetResult();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Allow_Origin_For_Get_Request()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");
        request.Headers.Add("Origin", "http://example.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be("*");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Respond_To_Preflight_Request()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/incidentreports");
        request.Headers.Add("Origin", "http://example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be("*");
        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Methods").First().Should().Contain("POST");
    }
}
