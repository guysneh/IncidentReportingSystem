using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.Comments;

public class CommentsErrorCasesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public CommentsErrorCasesTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact, Trait("Category", "Integration")]
    public async Task Get_For_Missing_Incident_Returns_404()
    {
        var user = _factory.AsUser();
        var missing = Guid.NewGuid();

        var res = await user.GetAsync(RouteHelper.R(_factory, $"incidentreports/{missing}/comments"));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task Post_For_Missing_Incident_Returns_404()
    {
        var c = await RegisterAndLoginAsync($"miss.{Guid.NewGuid():N}@example.com");
        var missing = Guid.NewGuid();

        var res = await c.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{missing}/comments"),
            new { text = "nope" });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task Delete_From_Wrong_Incident_Returns_404()
    {
        var c = await RegisterAndLoginAsync($"wrong.{Guid.NewGuid():N}@example.com");

        var a = await CreateIncidentAsync(c);
        var create = await c.PostAsJsonAsync(RouteHelper.R(_factory, $"incidentreports/{a}/comments"), new { text = "x" });
        create.EnsureSuccessStatusCode();
        var dto = await create.Content.ReadFromJsonAsync<CommentView>();
        var commentId = dto!.Id;

        var b = await CreateIncidentAsync(c);

        var del = await c.DeleteAsync(RouteHelper.R(_factory, $"incidentreports/{b}/comments/{commentId}"));
        Assert.Equal(HttpStatusCode.NotFound, del.StatusCode);
    }

    // helpers

    private async Task<HttpClient> RegisterAndLoginAsync(string email)
    {
        var c = _factory.CreateClient();
        var reg = await c.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"), new { Email = email, Password = "P@ssw0rd!", Roles = new[] { "User" } });
        Assert.True(reg.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

        var login = await c.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/login"), new { Email = email, Password = "P@ssw0rd!" });
        login.EnsureSuccessStatusCode();
        var dto = await login.Content.ReadFromJsonAsync<LoginDto>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto!.AccessToken);
        return c;
    }

    private async Task<Guid> CreateIncidentAsync(HttpClient c)
    {
        var res = await c.PostAsJsonAsync(RouteHelper.R(_factory, "incidentreports"), new
        {
            description = "d",
            location = "l",
            reporterId = Guid.NewGuid(),
            category = "ITSystems",
            systemAffected = "API",
            severity = "Medium",
            reportedAt = DateTime.UtcNow
        });
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<IncidentDto>();
        return dto!.Id;
    }

    private sealed class LoginDto { [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty; }
    private sealed class IncidentDto { public Guid Id { get; set; } }
    private sealed class CommentView { public Guid Id { get; set; } }
}
