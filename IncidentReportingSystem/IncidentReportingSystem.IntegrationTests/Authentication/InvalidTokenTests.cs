using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using IncidentReportingSystem.Application.Users.Commands.LoginUser;
using System.Net.Http.Json;
using static IncidentReportingSystem.IntegrationTests.Utils.CustomWebApplicationFactory;

namespace IncidentReportingSystem.Tests.Integration.Auth;

public class InvalidTokenTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public InvalidTokenTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("Bearer invalid.token.value")]
    [InlineData("invalid-token-without-bearer")]
    [Trait("Category", "Integration")]
    public async Task Should_Return_401_For_Invalid_Token(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, RouteHelper.R(_factory, "incidentreports"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_401_When_Missing_Token()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, RouteHelper.R(_factory, "incidentreports"));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
    {
        var payload = new LoginUserCommand("fake@example.com", "wrongpwd");

        var response = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/login"), payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
