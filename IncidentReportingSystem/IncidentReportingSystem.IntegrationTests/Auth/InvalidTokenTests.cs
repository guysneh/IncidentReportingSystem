using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.Tests.Integration.Auth;

public class InvalidTokenTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InvalidTokenTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("Bearer invalid.token.value")]
    [InlineData("invalid-token-without-bearer")]
    public async Task Should_Return_401_For_Invalid_Token(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Return_401_When_Missing_Token()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
