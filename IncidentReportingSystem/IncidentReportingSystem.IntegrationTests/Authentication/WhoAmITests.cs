using IncidentReportingSystem.IntegrationTests.Utils;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
        // Arrange: obtain an authenticated client via existing helpers
        var client = await TestClients.AsUserAsync(_factory, roles: new[] { "User" }, email: "user@test.local");

        // Act
        var res = await client.GetAsync(RouteHelper.R(_factory, "auth/me"));

        // Assert
        await res.ShouldBeAsync(HttpStatusCode.OK, _output, "WhoAmI with valid token");
        var dto = await res.Content.ReadFromJsonAsync<WhoAmIResponse>(Json);

        Assert.NotNull(dto);
        Assert.True(Guid.TryParse(dto!.UserId, out _), "userId should be a GUID");
        Assert.Equal("user@test.local", dto.Email);
        Assert.Contains("User", dto.Roles);
    }

    /// <summary>
    /// With single-role enforcement, /auth/me should still return 200 and include that role.
    /// </summary>
    [Fact]
    public async Task Me_Returns200_WithSingleRole()
    {
        var client = await TestClients.AsUserAsync(_factory, roles: new[] { "Admin" }, email: "admin@test.local");

        var res = await client.GetAsync(RouteHelper.R(_factory, "auth/me"));
        await res.ShouldBeAsync(HttpStatusCode.OK, _output);

        var dto = await res.Content.ReadFromJsonAsync<WhoAmIResponse>(Json);
        Assert.NotNull(dto);
        Assert.Contains("Admin", dto!.Roles);
        Assert.Equal("admin@test.local", dto.Email);
    }

    [Fact]
    public async Task Me_Returns401_WhenNoToken()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync(RouteHelper.R(_factory, "auth/me"));
        await res.ShouldBeAsync(HttpStatusCode.Unauthorized, _output);
    }


    /// <summary>
    /// When a user registers with first/last name, /auth/me should include those fields and a displayName.
    /// </summary>
    [Fact]
    public async Task Me_Includes_First_Last_DisplayName_When_Provided()
    {
        var email = $"whoami_{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd!";

        // 1) Register with names
        var registerPayload = new IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser.RegisterUserCommand(
            Email: email,
            Password: password,
            Roles: new[] { "User" },
            FirstName: "Jane",
            LastName: "Doe"
        );

        var anon = _factory.CreateClient();
        var reg = await anon.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), registerPayload);
        await reg.ShouldBeAsync(HttpStatusCode.Created, _output);

        // 2) Login to get JWT
        var loginBody = new { Email = email, Password = password };
        var login = await anon.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/login"), loginBody);
        await login.ShouldBeAsync(HttpStatusCode.OK, _output);

        var loginDto = await login.Content.ReadFromJsonAsync<LoginResponse>(Json);
        Assert.NotNull(loginDto);

        // 3) /auth/me with token
        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDto!.AccessToken);

        var me = await authed.GetAsync(RouteHelper.R(_factory, "auth/me"));
        await me.ShouldBeAsync(HttpStatusCode.OK, _output);

        var dto = await me.Content.ReadFromJsonAsync<WhoAmIResponseV2>(Json);
        Assert.NotNull(dto);

        Assert.True(Guid.TryParse(dto!.UserId, out _));
        Assert.Equal(email, dto.Email);
        Assert.Contains("User", dto.Roles);

        Assert.Equal("Jane", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("Jane Doe", dto.DisplayName);
    }

    // Local DTOs for deserialization in tests (avoid tight coupling on API assembly)
    private sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

    private sealed record WhoAmIResponseV2(
        string UserId,
        string Email,
        string[] Roles,
        string? FirstName,
        string? LastName,
        string? DisplayName
    );
    private sealed record WhoAmIResponse(string UserId, string Email, string[] Roles);
}
