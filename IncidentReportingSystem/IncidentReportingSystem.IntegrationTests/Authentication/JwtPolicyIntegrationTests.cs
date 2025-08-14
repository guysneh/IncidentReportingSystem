using System.Net;
using IncidentReportingSystem.IntegrationTests.Infrastructure;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.Authentication
{
    public sealed class JwtPolicyIntegrationTests
    {
        [Fact]
        public async Task SecureEndpoint_WithoutToken_Returns401()
        {
            using var factory = new SecureEndpointFactory();
            var client = factory.CreateClient();

            var resp = await client.GetAsync("/__test/secure");
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task SecureEndpoint_WithUserRole_Returns200()
        {
            using var factory = new SecureEndpointFactory();
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(factory, roles: new[] { "User" });

            var resp = await client.GetAsync("/__test/secure");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task SecureEndpoint_WithWrongRole_Returns403()
        {
            using var factory = new SecureEndpointFactory();
            // No User/Admin here -> should fail policy "CanReadIncidents"
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(factory, roles: new[] { "Guest" });

            var resp = await client.GetAsync("/__test/secure");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }
    }
}
