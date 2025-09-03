using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Users
{
    [Trait("Category", "Integration")]
    public sealed class UserProfileEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _f;
        private readonly ITestOutputHelper _o;

        public UserProfileEndpointsTests(CustomWebApplicationFactory f, ITestOutputHelper o)
        {
            _f = f; _o = o;
        }

        [Fact(DisplayName = "PATCH /auth/me updates first/last name and GET /auth/me reflects changes")]
        public async Task UpdateMe_Then_Me_Returns_Updated_Profile()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
            var root = await ApiRootResolver.ResolveAsync(_f, client);

            // PATCH
            var patchBody = new { firstName = "Guy", lastName = "Sne" };
            var res = await client.PatchAsJsonAsync($"{root}/auth/me", patchBody);
            await res.ShouldBeAsync(HttpStatusCode.OK, _o, "update profile should succeed");

            var dto = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
            dto.GetProperty("firstName").GetString().Should().Be("Guy");
            dto.GetProperty("lastName").GetString().Should().Be("Sne");
            dto.GetProperty("displayName").GetString().Should().Be("Guy Sne");

            // GET /auth/me should read DB and reflect new names
            var me = await client.GetAsync($"{root}/auth/me");
            await me.ShouldBeAsync(HttpStatusCode.OK, _o, "me should reflect DB values immediately");
            var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync()).RootElement;
            meJson.GetProperty("firstName").GetString().Should().Be("Guy");
            meJson.GetProperty("lastName").GetString().Should().Be("Sne");
        }

        [Theory(DisplayName = "PATCH /auth/me rejects invalid names (400)")]
        [InlineData("G@uy", "Sne")]
        [InlineData("", "Sne")]
        [InlineData("Guy", "")]
        public async Task UpdateMe_Invalid_400(string first, string last)
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
            var root = await ApiRootResolver.ResolveAsync(_f, client);

            var res = await client.PatchAsJsonAsync($"{root}/auth/me", new { firstName = first, lastName = last });
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "PATCH /auth/me without token returns 401")]
        public async Task UpdateMe_Unauthorized_401()
        {
            var client = _f.CreateClient(); // no token
            var root = await ApiRootResolver.ResolveAsync(_f, client);

            var res = await client.PatchAsJsonAsync($"{root}/auth/me", new { firstName = "A", lastName = "B" });
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
