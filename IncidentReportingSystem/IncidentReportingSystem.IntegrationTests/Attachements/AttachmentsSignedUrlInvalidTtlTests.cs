using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsSignedUrlInvalidTtlTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    public AttachmentsSignedUrlInvalidTtlTests(AttachmentsWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "download-url with invalid ttlMinutes returns 400")]
    public async Task Invalid_TTL_Returns_400()
    {
        var c = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, c);

        var res = await c.PostAsync($"{apiRoot}/attachments/{Guid.NewGuid()}/download-url?ttlMinutes=0", new StringContent(""));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
