using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

public sealed class AttachmentsList_EdgeCasesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AttachmentsList_EdgeCasesTests(CustomWebApplicationFactory f) => _factory = f;

    private sealed class Paged<T>
    {
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("skip")] public int Skip { get; set; }
        [JsonPropertyName("take")] public int Take { get; set; }
        [JsonPropertyName("items")] public List<T> Items { get; set; } = new();
    }

    private sealed class AttachmentView
    {
        public Guid id { get; set; }
        public string fileName { get; set; } = default!;
        public string contentType { get; set; } = default!;
        public string status { get; set; } = default!;
        public DateTimeOffset createdAt { get; set; }
        public DateTimeOffset? completedAt { get; set; }
    }

    [Fact]
    public async Task Unauthorized_Returns_401()
    {
        var c = _factory.CreateClient(); // no token
        var incidentId = Guid.NewGuid();
        var res = await c.GetAsync(RouteHelper.R(_factory, $"api/v1/incidentreports/{incidentId}/attachments"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Empty_List_Returns_Total_0_And_No_Items()
    {
        var c = await RegisterAndLoginAsync("User");
        var incidentId = Guid.NewGuid(); // no attachments seeded
        var paged = await c.GetFromJsonAsync<Paged<AttachmentView>>(
            RouteHelper.R(_factory, $"api/v1/incidentreports/{incidentId}/attachments?skip=0&take=5"));
        Assert.NotNull(paged);
        Assert.Equal(0, paged!.Total);
        Assert.Empty(paged.Items);
    }

    [Fact]
    public async Task Negative_Skip_And_NonPositive_Take_Are_Sanitized()
    {
        var c = await RegisterAndLoginAsync("User");
        var incidentId = await CreateIncidentAndSeedThreeAsync(c);

        var paged = await c.GetFromJsonAsync<Paged<AttachmentView>>(
            RouteHelper.R(_factory, $"api/v1/incidentreports/{incidentId}/attachments?skip=-10&take=0"));

        Assert.NotNull(paged);
        Assert.True(paged!.Take > 0);           // default applied by repo/handler
        Assert.Equal(3, paged.Total);
        Assert.True(paged.Items.Count <= paged.Take);
    }

    [Fact]
    public async Task Does_Not_Leak_Internal_StoragePath()
    {
        var c = await RegisterAndLoginAsync("User");
        var incidentId = await CreateIncidentAndSeedThreeAsync(c);

        var res = await c.GetAsync(RouteHelper.R(_factory, $"api/v1/incidentreports/{incidentId}/attachments?take=1"));
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();

        Assert.DoesNotContain("storagePath", json, StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Helpers ----------

    private async Task<Guid> CreateIncidentAndSeedThreeAsync(HttpClient c)
    {
        var incidentId = await CreateIncidentAsync(c);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userId = GetUserIdFromBearer(c) ?? Guid.NewGuid();

        var a1 = new Attachment(AttachmentParentType.Incident, incidentId, "f1.jpg", "image/jpeg", $"incidents/{incidentId}/{Guid.NewGuid()}/f1.jpg", userId);
        a1.MarkCompleted(6, false);
        await Task.Delay(5);
        var a2 = new Attachment(AttachmentParentType.Incident, incidentId, "f2.jpg", "image/jpeg", $"incidents/{incidentId}/{Guid.NewGuid()}/f2.jpg", userId);
        a2.MarkCompleted(7, false);
        await Task.Delay(5);
        var a3 = new Attachment(AttachmentParentType.Incident, incidentId, "f3.jpg", "image/jpeg", $"incidents/{incidentId}/{Guid.NewGuid()}/f3.jpg", userId);
        a3.MarkCompleted(8, false);

        db.Attachments.AddRange(a1, a2, a3);
        await db.SaveChangesAsync();
        return incidentId;
    }

    private async Task<HttpClient> RegisterAndLoginAsync(params string[] roles)
    => await AuthTestHelpers.RegisterAndLoginAsync(_factory, userId: null, roles: roles);

    private static Guid? GetUserIdFromBearer(HttpClient c)
        => JwtTestHelpers.ExtractUserId(c); // uses your existing helper

    private async Task<Guid> CreateIncidentAsync(HttpClient client)
    {
        var uid = GetUserIdFromBearer(client) ?? Guid.NewGuid();
        var body = new
        {
            description = "seed",
            location = "X",
            reporterId = uid,
            category = "PowerOutage",
            systemAffected = "SYS",
            severity = "Medium",
            reportedAt = DateTime.UtcNow
        };
        var res = await client.PostAsJsonAsync(RouteHelper.R(_factory, "api/v1/incidentreports"), body);
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<CreatedView>();
        return created!.id;
    }

    private sealed class CreatedView { public Guid id { get; set; } }
}
