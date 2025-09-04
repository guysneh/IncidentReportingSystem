using System.Net;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.IncidentReports
{
    [Trait("Category", "Integration")]
    public sealed class IncidentReports_TakeLimit_IntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public IncidentReports_TakeLimit_IntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact(DisplayName = "GET /incidentreports?take=999 returns 400 (validator)")]
        public async Task Take_Too_Big_Should_Return_400()
        {
            var client = _factory.AsUser();
            var res = await client.GetAsync(RouteHelper.R(_factory, "incidentreports?take=999"));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }
    }
}
