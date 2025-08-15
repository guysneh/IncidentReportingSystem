using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.Auth;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetToken_ShouldReturnJwtToken_WhenCalledWithValidParameters()
    {
        // Arrange
        var userId = "test-user";
        var role = "User";

        // Act
        var response = await _client.GetAsync($"/api/{TestConstants.ApiVersion}/auth/token?userId={userId}&role={role}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadAsStringAsync();
        token.Should().NotBeNullOrWhiteSpace();
        token.Should().Contain("."); 
    }
}
