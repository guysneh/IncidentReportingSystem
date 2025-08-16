using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using System.IdentityModel.Tokens.Jwt;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    public class LoginFlowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        private const string V = TestConstants.ApiVersion;

        public LoginFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task Register_Then_Login_Returns_200_And_Token()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string password = "P@ssw0rd!";

            var reg = await client.PostAsJsonAsync($"api/{V}/Auth/register", new
            {
                Email = email,
                Password = password,
                Roles = new[] { "User" }
            });
            Assert.True(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Contains(reg.StatusCode));

            // Login
            var login = await client.PostAsJsonAsync($"api/{V}/Auth/login", new { Email = email, Password = password });
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

            var reg = await client.PostAsJsonAsync($"api/{V}/Auth/register", new
            {
                Email = email,
                Password = password,
                Roles = new[] { "User" }
            });
            Assert.True(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Contains(reg.StatusCode));

            var login = await client.PostAsJsonAsync($"api/{V}/Auth/login", new { Email = email, Password = "WRONGWRONG" });
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }

        [Fact]
        public async Task Login_With_CorrectPassword_Returns_200()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string password = "P@ssw0rd!";

            var reg = await client.PostAsJsonAsync($"api/{V}/Auth/register", new
            {
                Email = email,
                Password = password,
                Roles = new[] { "User" }
            });
            Assert.True(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }.Contains(reg.StatusCode));

            var login = await client.PostAsJsonAsync($"api/{V}/Auth/login", new { Email = email, Password = password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }

        [Fact]
        public async Task Login_Returns_JWT_With_Expected_Claims()
        {
            var client = _factory.CreateClient();
            var email = $"login.{Guid.NewGuid():N}@example.com";
            const string pwd = "P@ssw0rd!";

            await client.PostAsJsonAsync("api/v1.0/Auth/register", new { Email = email, Password = pwd, Roles = new[] { "User" } });
            var login = await client.PostAsJsonAsync("api/v1.0/Auth/login", new { Email = email, Password = pwd });
            var dto = await login.Content.ReadFromJsonAsync<LoginResponseFile>();

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto!.AccessToken);

            Assert.Contains(jwt.Claims, c => c.Type == "sub");   // ClaimTypesConst.UserId
            Assert.Contains(jwt.Claims, c => c.Type == "email");
            Assert.Contains(jwt.Claims, c => c.Type == "role");
            Assert.True(jwt.ValidTo > DateTime.UtcNow);
        }

        private sealed class LoginResponseDto
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAtUtc { get; set; }
        }
    }
    file sealed class LoginResponseFile { public string AccessToken { get; set; } = ""; public DateTimeOffset ExpiresAtUtc { get; set; } }
}
