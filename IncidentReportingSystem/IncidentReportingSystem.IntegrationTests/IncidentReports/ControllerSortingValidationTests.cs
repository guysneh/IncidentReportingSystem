using System.Net;
using IncidentReportingSystem.IntegrationTests.Utils;
using static IncidentReportingSystem.IntegrationTests.Utils.CustomWebApplicationFactory;

namespace IncidentReportingSystem.IntegrationTests.IncidentReports
{
    public class ControllerSortingValidationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public ControllerSortingValidationTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact]
        public async Task InvalidEnumValue_Returns_400()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { "User" });
            var res = await client.GetAsync(RouteHelper.R(_factory, "IncidentReports?sortBy=NotARealField"));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }
    }
}
