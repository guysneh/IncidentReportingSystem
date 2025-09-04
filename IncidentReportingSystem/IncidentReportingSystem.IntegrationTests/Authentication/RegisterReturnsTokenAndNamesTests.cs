using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    [Trait("Category", "Integration")]
    public sealed class RegisterReturnsTokenAndNamesTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _f;
        private readonly ITestOutputHelper _o;

        public RegisterReturnsTokenAndNamesTests(CustomWebApplicationFactory f, ITestOutputHelper o)
        {
            _f = f; _o = o;
        }

        [Fact(DisplayName = "POST /auth/register returns token + names; /auth/me with that token returns same names")]
        public async Task Register_Returns_Token_And_Profile()
        {
            var client = _f.CreateClient();
            var root = await ApiRootResolver.ResolveAsync(_f, client);

            var email = $"user_{Guid.NewGuid():N}@example.com";
            var body = new
            {
                email,
                password = "P@ssw0rd!",
                firstName = "Guy",
                lastName = "Sne",
                roles = new[] { "User" }
            };

            var res = await client.PostAsJsonAsync($"{root.Replace("/users", "")}/auth/register", body);
            await res.ShouldBeAsync(HttpStatusCode.Created, _o, "register should return 201 with token + profile");

            var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
            var token = doc.GetProperty("accessToken").GetString();
            token.Should().NotBeNullOrWhiteSpace();
            doc.GetProperty("email").GetString().Should().Be(email);
            doc.GetProperty("firstName").GetString().Should().Be("Guy");
            doc.GetProperty("lastName").GetString().Should().Be("Sne");
            doc.GetProperty("displayName").GetString().Should().Be("Guy Sne");

            // call /auth/me with the returned token and verify names match
            var authed = _f.CreateClient();
            authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var me = await authed.GetAsync($"{root.Replace("/users", "")}/auth/me");
            await me.ShouldBeAsync(HttpStatusCode.OK, _o, "me with new token should work");
            var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync()).RootElement;

            meJson.GetProperty("email").GetString().Should().Be(email);
            meJson.GetProperty("firstName").GetString().Should().Be("Guy");
            meJson.GetProperty("lastName").GetString().Should().Be("Sne");
        }
    }
}
