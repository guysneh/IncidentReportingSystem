using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using System.IdentityModel.Tokens.Jwt;
using static IncidentReportingSystem.IntegrationTests.Utils.CustomWebApplicationFactory;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    public class LoginFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public LoginFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Register_Then_Login_Returns_200_And_Token()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string password = "P@ssw0rd!";

            var reg = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = email, Password = password, Roles = new[] { "User" } });
            Assert.True(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Contains(reg.StatusCode));

            var login = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/login"),
                new { Email = email, Password = password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.False(string.IsNullOrWhiteSpace(body?.AccessToken));
            Assert.True(body!.ExpiresAtUtc > DateTimeOffset.UtcNow);
        }

        [Fact]
        public async Task Login_With_WrongPassword_Returns_401()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string password = "P@ssw0rd!";

            var reg = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = email, Password = password, Roles = new[] { "User" } });
            Assert.True(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Contains(reg.StatusCode));

            var login = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/login"),
                new { Email = email, Password = "WRONGWRONG" });
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }

        [Fact]
        public async Task Login_With_CorrectPassword_Returns_200()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string password = "P@ssw0rd!";

            var reg = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = email, Password = password, Roles = new[] { "User" } });
            Assert.True(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Contains(reg.StatusCode));

            var login = await client.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/login"),
                new { Email = email, Password = password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }

        [Fact]
        public async Task Login_Returns_JWT_With_Expected_Claims()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string pwd = "P@ssw0rd!";

            await client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), new { Email = email, Password = pwd, Roles = new[] { "User" } });
            var login = await client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/login"), new { Email = email, Password = pwd });
            var dto = await login.Content.ReadFromJsonAsync<LoginResponseFile>();

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto!.AccessToken);

            Assert.Contains(jwt.Claims, c => c.Type == "sub");
            Assert.Contains(jwt.Claims, c => c.Type == "email");
            Assert.Contains(jwt.Claims, c => c.Type == "role");
            Assert.True(jwt.ValidTo > DateTime.UtcNow);
        }

        [Theory, Trait("Category", "Integration")]
        [InlineData("", "")]
        [InlineData("not-an-email", "x")]
        public async Task Login_Invalid_Input_Returns_400(string email, string pwd)
        {
            var c = _factory.CreateClient();

            var res = await c.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/login"),
                new { Email = email, Password = pwd });

            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact, Trait("Category", "Integration")]
        public async Task Login_Wrong_Password_Returns_401()
        {
            var c = _factory.CreateClient();
            var email = $"t.{Guid.NewGuid():N}@example.com";

            var r = await c.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/register"),
                new { Email = email, Password = "P@ssw0rd!", Roles = new[] { "User" } });

            Assert.True(r.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

            var res = await c.PostAsJsonAsync(
                RouteHelper.R(_factory, "Auth/login"),
                new { Email = email, Password = "wrongpassword" });

            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        private sealed class LoginResponseDto
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAtUtc { get; set; }
        }

        private sealed class LoginResponseFile
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAtUtc { get; set; }
        }
    }
}
