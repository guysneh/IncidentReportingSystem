using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    [Trait("Category", "Integration")]
    public sealed class ChangePasswordEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _f;
        private readonly ITestOutputHelper _o;

        public ChangePasswordEndpointTests(CustomWebApplicationFactory f, ITestOutputHelper o)
        {
            _f = f; _o = o;
        }

        private async Task<(Guid UserId, string Email)> RegisterAsync(HttpClient client, string email, string pass)
        {
            var root = await ApiRootResolver.ResolveAsync(_f, client);
            var res = await client.PostAsJsonAsync($"{root}/auth/register", new
            {
                email,
                password = pass,
                roles = new[] { "User" },
                firstName = "T",
                lastName = "U"
            });
            await res.ShouldBeAsync(HttpStatusCode.Created, _o, "register seed user");
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
            var id = json.TryGetProperty("id", out var idProp)
                ? idProp.GetGuid()
                : Guid.Parse(json.GetProperty("userId").GetString()!);
            return (id, email);
        }

        [Fact(DisplayName = "Change password success → 204; login with new password succeeds")]
        public async Task ChangePassword_Succeeds_Then_Login_With_New()
        {
            var anon = _f.CreateClient();
            var (uid, email) = await RegisterAsync(anon, $"user{Guid.NewGuid():N}@example.com", "OldGood1!");

            var authed = AuthenticatedHttpClientFactory.CreateClientWithToken(_f, userId: uid, email: email);
            var root = await ApiRootResolver.ResolveAsync(_f, authed);

            var res = await authed.PostAsJsonAsync($"{root}/auth/me/change-password", new
            {
                currentPassword = "OldGood1!",
                newPassword = "NewStrongP@ssw0rd!"
            });
            await res.ShouldBeAsync(HttpStatusCode.NoContent, _o, "change password should succeed");

            // Login with new password
            var login = await anon.PostAsJsonAsync($"{root}/auth/login", new { email, password = "NewStrongP@ssw0rd!" });
            await login.ShouldBeAsync(HttpStatusCode.OK, _o, "login with new password should succeed");
        }

        [Fact(DisplayName = "Change password with wrong current → 403/400")]
        public async Task ChangePassword_WrongCurrent_Fails()
        {
            var anon = _f.CreateClient();
            var (uid, email) = await RegisterAsync(anon, $"user{Guid.NewGuid():N}@example.com", "OldGood1!");

            var authed = AuthenticatedHttpClientFactory.CreateClientWithToken(_f, userId: uid, email: email);
            var root = await ApiRootResolver.ResolveAsync(_f, authed);

            var res = await authed.PostAsJsonAsync($"{root}/auth/me/change-password", new
            {
                currentPassword = "WRONG!",
                newPassword = "NewStrongP@ssw0rd!"
            });

            res.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "Change password with weak new password → 400")]
        public async Task ChangePassword_Weak_Fails()
        {
            var anon = _f.CreateClient();
            var (uid, email) = await RegisterAsync(anon, $"user{Guid.NewGuid():N}@example.com", "OldGood1!");

            var authed = AuthenticatedHttpClientFactory.CreateClientWithToken(_f, userId: uid, email: email);
            var root = await ApiRootResolver.ResolveAsync(_f, authed);

            var res = await authed.PostAsJsonAsync($"{root}/auth/me/change-password", new
            {
                currentPassword = "OldGood1!",
                newPassword = "weak" // < 12, no complexity
            });

            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
