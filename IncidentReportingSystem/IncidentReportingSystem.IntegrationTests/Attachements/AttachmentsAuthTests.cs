using System.Net;
using System.Text;
using System.Text.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("TestType", "Integration")]
public sealed class AttachmentsAuthTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public AttachmentsAuthTests(AttachmentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact(DisplayName = "Upload-form without auth -> 401")]
    public async Task UploadForm_Unauthorized_401()
    {
        var client = _factory.CreateClient(); // no token
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("incidents/unauth/whatever.jpg"), "path");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("x")), "file", "whatever.jpg");

        var res = await client.PostAsync($"{apiRoot}/attachments/_loopback/upload-form", form);
        await res.ShouldBeAsync(HttpStatusCode.Unauthorized, _output, "upload-form requires auth");
    }

    [Fact(DisplayName = "Binary upload without auth -> 401")]
    public async Task UploadBinary_Unauthorized_401()
    {
        var client = _factory.CreateClient();
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var res = await client.PutAsync($"{apiRoot}/attachments/_loopback/upload?path=incidents/x/unauth.jpg",
            new ByteArrayContent(Encoding.UTF8.GetBytes("x")));
        await res.ShouldBeAsync(HttpStatusCode.Unauthorized, _output, "binary upload requires auth");
    }

    [Fact(DisplayName = "Complete without auth -> 401")]
    public async Task Complete_Unauthorized_401()
    {
        // prepare attachment ID using authorized client
        var authed = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory);
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, authed);
        var incidentId = KnownIds.ExistingIncidentId(_factory);

        var start = await authed.PostAsync(
            $"{apiRoot}/incidentreports/{incidentId}/attachments/start",
            new StringContent(JsonSerializer.Serialize(new { fileName = $"noauth-{Guid.NewGuid():N}.png", contentType = "image/png" }),
            Encoding.UTF8, "application/json"));
        await start.ShouldBeAsync(HttpStatusCode.OK, _output, "Start");

        using var doc = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        var attachmentId = doc.RootElement.GetProperty("attachmentId").GetGuid();

        // act with unauthenticated client
        var anon = _factory.CreateClient();
        var res = await anon.PostAsync($"{apiRoot}/attachments/{attachmentId}/complete", new StringContent(""));
        await res.ShouldBeAsync(HttpStatusCode.Unauthorized, _output, "complete requires auth");
    }
}
