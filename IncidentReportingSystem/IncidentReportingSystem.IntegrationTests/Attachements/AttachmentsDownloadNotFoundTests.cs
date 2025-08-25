using System.Net;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsDownloadNotFoundTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _f;
    private readonly ITestOutputHelper _o;
    public AttachmentsDownloadNotFoundTests(AttachmentsWebApplicationFactory f, ITestOutputHelper o)
    { _f = f; _o = o; }

    [Fact(DisplayName = "Download non-existing attachment → 404")]
    public async Task Download_NotFound_404()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_f);
        var root = await ApiRootResolver.ResolveAsync(_f, client);

        var missing = Guid.NewGuid();
        var res = await client.GetAsync($"{root}/attachments/{missing}/download");
        await res.ShouldBeAsync(HttpStatusCode.NotFound, _o, "unknown id should return 404");
    }
}