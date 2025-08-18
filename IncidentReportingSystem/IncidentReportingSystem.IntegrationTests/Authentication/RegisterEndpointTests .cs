using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.Application.Users.Commands.RegisterUser;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    public class RegisterEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public RegisterEndpointTests(CustomWebApplicationFactory factory)
        {
            _client = factory.AsAdmin();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns201_On_Valid_Input()
        {
            var email = $"alice_{Guid.NewGuid():N}@example.com"; 
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User" });

            var resp = await _client.PostAsJsonAsync($"api/{TestConstants.ApiVersion}/Auth/register", payload);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns409_On_Duplicate_Email()
        {
            var email = $"bob_{Guid.NewGuid():N}@example.com"; 
            var payload = new RegisterUserCommand(email, "P@ssw0rd!", new[] { "User" });

            var first = await _client.PostAsJsonAsync($"api/{TestConstants.ApiVersion}/Auth/register", payload);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await _client.PostAsJsonAsync($"api/{TestConstants.ApiVersion}/Auth/register", payload);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns400_On_Invalid_Role()
        {
            var payload = new RegisterUserCommand("carol.integration@example.com", "P@ssw0rd!", new[] { "NotARole" });

            var resp = await _client.PostAsJsonAsync($"api/{TestConstants.ApiVersion}/Auth/register", payload);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Register_Returns400_On_Invalid_Email_And_ShortPassword()
        {
            var badEmail = new RegisterUserCommand("bad", "P@ssw0rd!", new[] { "User" });
            var r1 = await _client.PostAsJsonAsync($"api/{TestConstants.ApiVersion}/Auth/register", badEmail);
            Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);

            var shortPw = new RegisterUserCommand("dave.integration@example.com", "short", new[] { "User" });
            var r2 = await _client.PostAsJsonAsync($"api/{TestConstants.ApiVersion}/Auth/register", shortPw);
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        }
    }
}
