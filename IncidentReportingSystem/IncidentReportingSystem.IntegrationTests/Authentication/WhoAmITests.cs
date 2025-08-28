using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Authentication;

public sealed class WhoAmITests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public WhoAmITests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Me_Returns200_WithUserInfo_WhenAuthorized()
    {
        // Arrange
        var client =  TestClientRoles.AsUser( _factory);

        // Act
        var url = RouteHelper.R(_factory, "auth/me");
        var res = await client.GetAsync(url);

        // Assert
        await res.ShouldBeAsync(HttpStatusCode.OK, _output, "WhoAmI with valid token");
        var dto = await res.Content.ReadFromJsonAsync<WhoAmIResponse>(Json);
        Assert.NotNull(dto);
        Assert.True(Guid.TryParse(dto!.UserId, out _), "userId should be a GUID");
        Assert.NotNull(dto.Email);
        Assert.Contains("User", dto.Roles);
    }

    [Fact]
    public async Task Me_Returns401_WhenNoToken()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync(RouteHelper.R(_factory, "auth/me"));
        await res.ShouldBeAsync(HttpStatusCode.Unauthorized, _output);
    }

    private sealed record WhoAmIResponse(string UserId, string Email, string[] Roles);
}
