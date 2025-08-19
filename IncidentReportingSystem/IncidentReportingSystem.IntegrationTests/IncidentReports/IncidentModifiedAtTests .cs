using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.IncidentReports;

public class IncidentModifiedAtTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public IncidentModifiedAtTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact, Trait("Category", "Integration")]
    public async Task ModifiedAt_Updates_On_Comment_Create()
    {
        var client = await RegisterAndLoginAsync($"mod.create.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(client);

        var before = await GetTimestampsAsync(client, incidentId); // modifiedAt may be null
        await Task.Delay(50); // avoid same-tick timestamps in DB

        var create = await client.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "hello" });
        create.EnsureSuccessStatusCode();

        var after = await GetTimestampsAsync(client, incidentId);

        var baseline = before.ModifiedAt ?? before.CreatedAt;
        Assert.NotNull(after.ModifiedAt);
        Assert.True(after.ModifiedAt!.Value >= baseline,
            $"Expected modifiedAt ({after.ModifiedAt:o}) >= baseline ({baseline:o}).");
        if (before.ModifiedAt is not null)
            Assert.NotEqual(before.ModifiedAt.Value, after.ModifiedAt.Value);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task ModifiedAt_Updates_On_Comment_Delete()
    {
        var client = await RegisterAndLoginAsync($"mod.delete.{Guid.NewGuid():N}@example.com", "User");
        var incidentId = await CreateIncidentAsync(client);

        // Create a comment so we have something to delete
        var create = await client.PostAsJsonAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments"),
            new { text = "to be deleted" });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CommentView>();
        var commentId = created!.Id;

        // Baseline after creation (modifiedAt should already be set, but we handle null just in case)
        var before = await GetTimestampsAsync(client, incidentId);
        await Task.Delay(50);

        var del = await client.DeleteAsync(
            RouteHelper.R(_factory, $"incidentreports/{incidentId}/comments/{commentId}"));
        del.EnsureSuccessStatusCode();

        var after = await GetTimestampsAsync(client, incidentId);

        var baseline = before.ModifiedAt ?? before.CreatedAt;
        Assert.NotNull(after.ModifiedAt);
        Assert.True(after.ModifiedAt!.Value >= baseline,
            $"Expected modifiedAt ({after.ModifiedAt:o}) >= baseline ({baseline:o}).");
        if (before.ModifiedAt is not null)
            Assert.NotEqual(before.ModifiedAt.Value, after.ModifiedAt.Value);
    }

    // ---------- helpers ----------

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
            description = "x",
            location = "y",
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

    private async Task<Timestamps> GetTimestampsAsync(HttpClient c, Guid incidentId)
    {
        var res = await c.GetAsync(RouteHelper.R(_factory, $"incidentreports/{incidentId}"));
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        return ExtractTimestamps(json);
    }

    private static Timestamps ExtractTimestamps(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var created = FindDate(root, new[] { "createdAtUtc", "createdAt" })
                      ?? throw new InvalidOperationException("Incident JSON is missing 'createdAt'.");

        var modified = FindDate(root, new[]
        {
            "modifiedAtUtc","modifiedAt",
            "updatedAtUtc","updatedAt",
            "lastModifiedAtUtc","lastModifiedAt"
        });

        return new Timestamps(created, modified);
    }

    private static DateTimeOffset? FindDate(JsonElement el, string[] names)
    {
        foreach (var p in el.EnumerateObject())
        {
            if (names.Any(n => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)))
            {
                if (TryReadDate(p.Value, out var dto)) return dto;
                if (p.Value.ValueKind == JsonValueKind.Null) return null;
            }
        }
        return null;
    }

    private static bool TryReadDate(JsonElement val, out DateTimeOffset dto)
    {
        if (val.ValueKind == JsonValueKind.String)
        {
            var s = val.GetString();
            if (!string.IsNullOrWhiteSpace(s) && DateTimeOffset.TryParse(s, out dto)) return true;
        }
        if (val.ValueKind == JsonValueKind.Number && val.TryGetInt64(out var n))
        {
            dto = n >= 1_000_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(n)
                : DateTimeOffset.FromUnixTimeSeconds(n);
            return true;
        }
        dto = default;
        return false;
    }

    private readonly record struct Timestamps(DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt);

    private sealed class LoginDto
    {
        [JsonPropertyName("accessToken")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expiresAtUtc")] public DateTimeOffset ExpiresAtUtc { get; set; }
    }

    private sealed class IncidentDto { public Guid Id { get; set; } }
    private sealed class CommentView { public Guid Id { get; set; } }
}
