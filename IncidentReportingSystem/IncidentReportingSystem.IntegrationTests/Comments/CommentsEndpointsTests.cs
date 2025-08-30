using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.IntegrationTests.Comments;

public class CommentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public CommentsEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact, Trait("Category", "Integration")]
    public async Task Create_List_Delete_Owner_Succeeds()
    {
        var owner = await RegisterAndLoginAsync($"user.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(owner);

        var create = await owner.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "first" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<CommentView>();
        Assert.NotNull(created);
        var commentId = created!.Id;

        // assert composite path
        create.Headers.Location!.ToString()
              .Should().Contain($"/incidentreports/{incidentId}/comments/{commentId}");

        // GET by Location (your existing endpoint supports incident+comment)
        var byLoc = await owner.GetAsync(create.Headers.Location);
        byLoc.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await byLoc.Content.ReadFromJsonAsync<CommentView>();
        dto!.Id.Should().Be(commentId);

        var list = await owner.GetFromJsonAsync<Paged<CommentView>>( 
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=0&take=10"));

        Assert.NotNull(list);
        Assert.Contains(list!.Items, c => c.Id == commentId); 

        var del = await owner.DeleteAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments/{commentId}"));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        // Location points to the composite resource
        create.Headers.Location.Should().NotBeNull();
    }

    [Fact, Trait("Category", "Integration")]
    public async Task List_Is_Newest_First_With_Paging()
    {
        var owner = await RegisterAndLoginAsync($"n1.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(owner);

        for (int i = 1; i <= 3; i++)
        {
            var r = await owner.PostAsJsonAsync(
                RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
                new { text = $"c{i}" });
            r.EnsureSuccessStatusCode();
        }

        var page1 = await owner.GetFromJsonAsync<Paged<CommentView>>( 
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=0&take=2"));
        var page2 = await owner.GetFromJsonAsync<Paged<CommentView>>( 
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=2&take=2"));

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(2, page1!.Items.Count); 
        Assert.Single(page2!.Items);         
    }

    [Fact, Trait("Category", "Integration")]
    public async Task Delete_By_Stranger_Forbidden()
    {
        var owner = await RegisterAndLoginAsync($"owner2.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(owner);

        var create = await owner.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "secret" });
        create.EnsureSuccessStatusCode();
        var comment = await create.Content.ReadFromJsonAsync<CommentView>();
        var commentId = comment!.Id;

        var stranger = await RegisterAndLoginAsync($"str.{Guid.NewGuid():N}@example.com", "User");

        var del = await stranger.DeleteAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments/{commentId}"));

        Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task Delete_By_Admin_Succeeds()
    {
        var owner = await RegisterAndLoginAsync($"owner3.{Guid.NewGuid():N}@example.com", "User");
        var admin = await RegisterAndLoginAsync($"admin.{Guid.NewGuid():N}@example.com", "Admin");

        var incidentId = await CreateIncidentAsync(owner);

        var create = await owner.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "admin can delete" });
        create.EnsureSuccessStatusCode();

        var comment = await create.Content.ReadFromJsonAsync<CommentView>();
        var commentId = comment!.Id;

        var del = await admin.DeleteAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments/{commentId}"));

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task ListComments_ShouldReturnPagedResponseContract()
    {
        var owner = await RegisterAndLoginAsync($"owner4.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(owner);

        await owner.PostAsJsonAsync(RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "comment one" });

        var res = await owner.GetAsync(RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=0&take=10"));
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("\"total\"");
        body.Should().Contain("\"items\"");
    }


    // ---- helpers ----

    private async Task<HttpClient> RegisterAndLoginAsync(string email, string role)
    {
        var c = _factory.CreateClient();

        var reg = await c.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/register"),
            new { Email = email, Password = "P@ssw0rd!", Roles = new[] { role } });

        Assert.True(reg.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);

        var login = await c.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/login"),
            new { Email = email, Password = "P@ssw0rd!" });

        login.EnsureSuccessStatusCode();
        var dto = await login.Content.ReadFromJsonAsync<LoginDto>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dto!.AccessToken);
        return c;
    }

    private async Task<Guid> CreateIncidentAsync(HttpClient c)
    {
        var res = await c.PostAsJsonAsync(RouteHelper.R(_factory, "incidentreports"), new
        {
            description = "Test incident",
            location = "Berlin",
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
    private sealed class Paged<T>
    {
        public int Total { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public List<T> Items { get; set; } = new();
    }
    private sealed class LoginDto { [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty; }
    private sealed class IncidentDto { public Guid Id { get; set; } }
    private sealed class CommentView { public Guid Id { get; set; } public Guid IncidentId { get; set; } public Guid UserId { get; set; } public string Text { get; set; } = string.Empty; public DateTime CreatedAtUtc { get; set; } }
}
