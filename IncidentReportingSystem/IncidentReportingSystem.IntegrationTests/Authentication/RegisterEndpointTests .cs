using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using IncidentReportingSystem.Application.Users.Commands.RegisterUser;
using IncidentReportingSystem.IntegrationTests.Utils;
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
    }
}
