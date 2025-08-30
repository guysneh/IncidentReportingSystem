using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

[Trait("Category", "Integration")]
public sealed class AttachmentsPrivacyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AttachmentsPrivacyTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "List by incident does not expose storagePath")]
    public async Task List_ByIncident_Does_Not_Expose_StoragePath()
    {
        var client = await RegisterAndLoginAsync("User");
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        // Create incident for current user
        var incidentId = await CreateIncidentAsync(client);

        // Seed completed attachments directly (no upload needed)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userId = GetUserIdFromBearer(client)!.Value;

            var a = new Attachment(
                AttachmentParentType.Incident,
                incidentId,
                "proof.pdf",
                "application/pdf",
                $"incidents/{incidentId}/{Guid.NewGuid()}/proof.pdf",
                userId);
            a.MarkCompleted(42);

            db.Attachments.Add(a);
            await db.SaveChangesAsync();
        }

        var res = await client.GetAsync($"{apiRoot}/incidentreports/{incidentId}/attachments?skip=0&take=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadAsStringAsync();
        json.Should().NotContain("storagePath", "internal storage keys must not be exposed in public DTOs");

        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToArray();
        items.Should().NotBeEmpty();
        foreach (var it in items)
        {
            it.TryGetProperty("storagePath", out _).Should().BeFalse();
        }
    }

    // --- helpers (same style as in other tests) ---

    private async Task<HttpClient> RegisterAndLoginAsync(params string[] roles)
    {
        var clientBootstrap = _factory.CreateClient();

        var email = $"{Guid.NewGuid():N}@example.com";
        var payloadRoles = (roles is { Length: > 0 }) ? roles : new[] { "User" };

        var reg = await clientBootstrap.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/register"),
            new { Email = email, Password = "P@ssw0rd!", Roles = payloadRoles });

        reg.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);

        var login = await clientBootstrap.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/login"),
            new { Email = email, Password = "P@ssw0rd!" });

        login.EnsureSuccessStatusCode();

        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())?.AccessToken
                    ?? (await login.Content.ReadAsStringAsync()).Trim('"');

        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return authed;
    }

    private sealed class LoginResponse
    {
        [JsonPropertyName("accessToken")] public string? AccessToken { get; set; }
    }

    private async Task<Guid> CreateIncidentAsync(HttpClient client)
    {
        var userId = GetUserIdFromBearer(client)!.Value;
        var body = new
        {
            description = "No-storagePath exposure test",
            location = "X",
            reporterId = userId,
            category = "PowerOutage",
            systemAffected = "SYS",
            severity = "Low",
            reportedAt = DateTime.UtcNow
        };

        var res = await client.PostAsJsonAsync(RouteHelper.R(_factory, "incidentreports"), body);
        res.EnsureSuccessStatusCode();

        var created = await res.Content.ReadFromJsonAsync<CreatedView>();
        return created!.id;
    }

    private sealed class CreatedView { public Guid id { get; set; } }

    private static Guid? GetUserIdFromBearer(HttpClient client)
    {
        var auth = client.DefaultRequestHeaders.Authorization;
        if (auth?.Parameter is null) return null;

        var parts = auth.Parameter.Split('.');
        if (parts.Length < 2) return null;

        static byte[] B64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + ((4 - s.Length % 4) % 4), '='));
        }

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(B64(parts[1]));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("sub", out var v) && Guid.TryParse(v.GetString(), out var g)) return g;
            if (root.TryGetProperty("nameidentifier", out var v2) && Guid.TryParse(v2.GetString(), out var g2)) return g2;
        }
        catch { }
        return null;
    }
}
