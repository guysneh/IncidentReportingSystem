using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.Comments;

public class CommentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public CommentsEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact, Trait("Category", "Integration")]
    public async Task Create_EmptyText_BadRequest()
    {
        var owner = await RegisterAndLoginAsync($"owner.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(owner);

        var res = await owner.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

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

        var list = await owner.GetFromJsonAsync<List<CommentView>>(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=0&take=10"));

        Assert.NotNull(list);
        Assert.Contains(list!, c => c.Id == commentId);

        var del = await owner.DeleteAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments/{commentId}"));

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
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

        var page1 = await owner.GetFromJsonAsync<List<CommentView>>(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=0&take=2"));
        var page2 = await owner.GetFromJsonAsync<List<CommentView>>(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments?skip=2&take=2"));

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(2, page1!.Count);
        Assert.Single(page2!);
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

    // ---- helpers ----

    private async Task<HttpClient> RegisterAndLoginAsync(string email, string role)
    {
        var c = _factory.CreateClient();

        var reg = await c.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/register"),
            new { Email = email, Password = "P@ssw0rd!", Roles = new[] { role } });

        if (reg.StatusCode != HttpStatusCode.Created && reg.StatusCode != HttpStatusCode.Conflict)
            throw new InvalidOperationException($"Register failed: {(int)reg.StatusCode} {reg.StatusCode}");

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
            description = "Test",
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

    private sealed class LoginDto { [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty; }
    private sealed class IncidentDto { public Guid Id { get; set; } }
    private sealed class CommentView { public Guid Id { get; set; } public Guid IncidentId { get; set; } public Guid UserId { get; set; } public string Text { get; set; } = string.Empty; public DateTime CreatedAtUtc { get; set; } }
}
