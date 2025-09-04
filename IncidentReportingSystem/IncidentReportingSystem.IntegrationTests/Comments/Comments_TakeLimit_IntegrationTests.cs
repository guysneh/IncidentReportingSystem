using System;
using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Comments
{
    [Trait("Category", "Integration")]
    public sealed class Comments_TakeLimit_IntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        public Comments_TakeLimit_IntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

        [Fact(DisplayName = "GET /incidentreports/{id}/comments?take=999 returns 400 (validator)")]
        public async Task Take_Too_Big_Should_Return_400()
        {
            var client = _factory.AsUser();

            // Create an incident to attach comments route to
            var createIncident = await client.PostAsJsonAsync(RouteHelper.R(_factory, "incidentreports"), new
            {
                description = "for comments take-limit test",
                location = "Berlin",
                reporterId = Guid.NewGuid(),
                category = "ITSystems",
                systemAffected = "API",
                severity = "Low",
                reportedAt = DateTime.UtcNow
            });
            createIncident.EnsureSuccessStatusCode();
            var incident = await createIncident.Content.ReadFromJsonAsync<IncidentIdDto>();

            var res = await client.GetAsync(RouteHelper.R(_factory, $"incidentreports/{incident!.Id}/comments?skip=0&take=999"));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        private sealed class IncidentIdDto { public Guid Id { get; set; } }
    }
}
