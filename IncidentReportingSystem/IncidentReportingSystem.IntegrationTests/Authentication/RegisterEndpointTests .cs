using FluentAssertions;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using IncidentReportingSystem.Application.Users.Commands.RegisterUser;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using static IncidentReportingSystem.IntegrationTests.Utils.CustomWebApplicationFactory;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    public class RegisterEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public RegisterEndpointTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.AsAdmin();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns201_On_Valid_Input()
        {
            var email = $"alice_{Guid.NewGuid():N}@example.com";
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User" });

            var resp = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns409_On_Duplicate_Email()
        {
            var email = $"bob_{Guid.NewGuid():N}@example.com";
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User" });

            var first = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns400_On_Invalid_Role()
        {
            var payload = new RegisterUserCommand("carol.integration@example.com", "P@ssw0rd!", new[] { "NotARole" });

            var resp = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns400_On_Invalid_Email_And_ShortPassword()
        {
            var badEmail = new RegisterUserCommand("bad", "P@ssw0rd!", new[] { "User" });
            var r1 = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), badEmail);
            Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);

            var shortPw = new RegisterUserCommand("dave.integration@example.com", "short", new[] { "User" });
            var r2 = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), shortPw);
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        }

        [Fact, Trait("Category", "Integration")]
        public async Task Register_Duplicate_Email_Returns_409()
        {
            var c = _factory.CreateClient();
            var email = $"dup.{Guid.NewGuid():N}@example.com";

            var r1 = await c.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = email, Password = "P@ssw0rd!", Roles = new[] { "User" } });

            Assert.Equal(HttpStatusCode.Created, r1.StatusCode);

            var r2 = await c.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = email, Password = "P@ssw0rd!", Roles = new[] { "User" } });

            Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
        }

        [Fact, Trait("Category", "Integration")]
        public async Task Register_Invalid_Role_Returns_400()
        {
            var c = _factory.CreateClient();

            var res = await c.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = $"inv.{Guid.NewGuid():N}@example.com", Password = "P@ssw0rd!", Roles = new[] { "Nope" } });

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Accepts_FirstAndLastName_And_Persists_Trimmed_With_DisplayName()
        {
            var email = $"eve_{Guid.NewGuid():N}@example.com";
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User" }, "  Eve  ", "  Adams ");

            var resp = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);

            user.FirstName.Should().Be("Eve");
            user.LastName.Should().Be("Adams");
            user.DisplayName.Should().Be("Eve Adams"); // default computed when no explicit DisplayName provided
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_NoNames_Persists_DisplayName_As_Email()
        {
            var email = $"nonames_{Guid.NewGuid():N}@example.com";
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User" }, null, null);

            var resp = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == email);

            user.FirstName.Should().BeNull();
            user.LastName.Should().BeNull();
            user.DisplayName.Should().Be(email); // email fallback
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns400_On_Multiple_Roles()
        {
            var email = $"multi_{Guid.NewGuid():N}@example.com";
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User", "Admin" });

            var resp = await _client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), payload);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Legacy_SingleRole_Works()
        {
            var client = _factory.CreateClient();
            var res = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "api/v1/auth/register"),
                new { Email = $"{Guid.NewGuid():N}@example.com", Password = "P@ssw0rd!", Role = "User" });
            res.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_New_MultiRoles_Works()
        {
            var client = _factory.CreateClient();
            var res = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "api/v1/auth/register"),
                new { Email = $"{Guid.NewGuid():N}@example.com", Password = "P@ssw0rd!", Roles = new[] { "User" }, FirstName = "A", LastName = "B" });
            res.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
        }
    }
}
