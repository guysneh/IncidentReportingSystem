using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.Tests.Integration.CORS;

public class CORSTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CORSTests(CustomWebApplicationFactory factory)
    {
        // Tokened client as before
        _client = AuthenticatedHttpClientFactory.CreateClientWithTokenAsync(factory).GetAwaiter().GetResult();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Allow_Configured_Origin_For_Get_Request()
    {
        // Arrange
        var origin = "http://example.com"; // must match Cors:AllowedOrigins set in factory
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");
        request.Headers.Add("Origin", origin);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue("CORS should add ACAO when origin is allowed");
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be(origin);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Respond_To_Preflight_Request_For_Configured_Origin()
    {
        // Arrange
        var origin = "http://example.com"; // must match Cors:AllowedOrigins set in factory
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/incidentreports");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Origin").First().Should().Be(origin);

        response.Headers.Contains("Access-Control-Allow-Methods").Should().BeTrue();
        response.Headers.GetValues("Access-Control-Allow-Methods").First().Should().Contain("POST");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Not_Allow_Unconfigured_Origin()
    {
        // Arrange
        var disallowedOrigin = "http://not-allowed.example";
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");
        request.Headers.Add("Origin", disallowedOrigin);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse("No ACAO header should be emitted for disallowed origin");
    }
}
