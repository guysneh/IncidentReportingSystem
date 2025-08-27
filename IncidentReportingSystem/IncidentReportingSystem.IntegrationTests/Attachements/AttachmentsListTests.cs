using IncidentReportingSystem.Domain;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments;

public sealed class AttachmentsListTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AttachmentsListTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    [Fact, Trait("Category", "Integration")]
    public async Task List_ByIncident_Returns_Paged_NewestFirst()
    {
        // Arrange: register+login & create an incident (כמו שיש לך)
        var client = await RegisterAndLoginAsync("Admin");
        var incidentId = await CreateIncidentAsync(client);

        // Seed 3 completed attachments directly via the domain constructor
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Extract current user id from JWT to satisfy domain invariants
            var uploadedBy = GetUserIdFromBearer(client)
                             ?? throw new InvalidOperationException("Missing user id in JWT.");

            // NOTE: We use the domain constructor and then mark as completed.
            // StoragePath must be a valid relative path; it does NOT need to exist for listing.
            var now = DateTimeOffset.UtcNow;

            var a1 = new Attachment(
                AttachmentParentType.Incident,
                incidentId,
                "file1.jpg",
                "image/jpeg",
                $"incidents/{incidentId}/{Guid.NewGuid()}/file1.jpg",
                uploadedBy);
            a1.MarkCompleted(size: 6, hasThumbnail: false);

            // Small delays ensure CreatedAt ordering (newest-first)
            await Task.Delay(10);
            var a2 = new Attachment(
                AttachmentParentType.Incident,
                incidentId,
                "file2.jpg",
                "image/jpeg",
                $"incidents/{incidentId}/{Guid.NewGuid()}/file2.jpg",
                uploadedBy);
            a2.MarkCompleted(size: 7, hasThumbnail: false);

            await Task.Delay(10);
            var a3 = new Attachment(
                AttachmentParentType.Incident,
                incidentId,
                "file3.jpg",
                "image/jpeg",
                $"incidents/{incidentId}/{Guid.NewGuid()}/file3.jpg",
                uploadedBy);
            a3.MarkCompleted(size: 8, hasThumbnail: false);

            db.Attachments.AddRange(a1, a2, a3);
            await db.SaveChangesAsync();
        }

        // Act – page size 2, then page 2 (skip 2)
        var page1 = await client.GetFromJsonAsync<Paged<AttachmentView>>(
            RouteHelper.R(_factory, $"api/v1/incidentreports/{incidentId}/attachments?skip=0&take=2"));
        var page2 = await client.GetFromJsonAsync<Paged<AttachmentView>>(
            RouteHelper.R(_factory, $"api/v1/incidentreports/{incidentId}/attachments?skip=2&take=2"));

        // Assert
        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(3, page1!.Total);
        Assert.Equal(3, page2!.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);

        // Newest-first: first item in page1 should be >= last item in page2
        Assert.True(page1.Items.First().createdAt >= page2.Items.Last().createdAt);
    }

    /// <summary>
    /// Registers a user with the provided roles, logs in, and returns an HttpClient
    /// pre-configured with the Bearer token. Defaults to "User" if no roles are provided.
    /// </summary>
    private async Task<HttpClient> RegisterAndLoginAsync(params string[] roles)
    {
        var clientBootstrap = _factory.CreateClient();

        var email = $"{Guid.NewGuid():N}@example.com";
        var payloadRoles = (roles is { Length: > 0 }) ? roles : new[] { "User" };

        // 1) Register with explicit roles so policies will match during tests
        var reg = await clientBootstrap.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/register"),
            new { Email = email, Password = "P@ssw0rd!", Roles = payloadRoles });

        Assert.True(reg.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict,
            $"Register failed. Status: {(int)reg.StatusCode} {reg.StatusCode}");

        // 2) Login to receive JWT
        var login = await clientBootstrap.PostAsJsonAsync(
            RouteHelper.R(_factory, "Auth/login"),
            new { Email = email, Password = "P@ssw0rd!" });

        login.EnsureSuccessStatusCode();

        // Try JSON { accessToken: "...", ... }, fallback to raw string
        var loginJson = await TryReadAs<LoginResponse>(login);
        var token = loginJson?.AccessToken ?? (await login.Content.ReadAsStringAsync()).Trim('"');

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Login did not return a valid JWT access token.");

        // 3) Return client authorized with Bearer token
        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return authed;
    }

    private sealed class LoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("expiresAtUtc")]
        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }

    private static async Task<T?> TryReadAs<T>(HttpResponseMessage res)
    {
        try { return await res.Content.ReadFromJsonAsync<T>(); }
        catch { return default; }
    }

    private async Task<Guid> CreateIncidentAsync(HttpClient client)
    {
        // 1) Extract current user id from JWT (sub/nameidentifier)
        var userId = GetUserIdFromBearer(client)
                     ?? throw new InvalidOperationException("Missing JWT or user id claim.");

        // 2) Send enums as strings (to match your API's expected input format)
        var body = new
        {
            description = "Attachments list test",
            location = "X",
            reporterId = userId,
            category = "PowerOutage",   // IncidentCategory.PowerOutage -> string
            systemAffected = "SYS",
            severity = "Medium",        // IncidentSeverity.Medium -> string
            reportedAt = DateTime.UtcNow
        };

        var res = await client.PostAsJsonAsync(RouteHelper.R(_factory, "incidentreports"), body);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"CreateIncident failed: {(int)res.StatusCode} {res.StatusCode}\n{err}");
        }

        var created = await res.Content.ReadFromJsonAsync<CreatedView>();
        Assert.NotNull(created);
        return created!.id;
    }

    /// <summary>
    /// Try to pull the current user's Guid from the Bearer token (sub/nameidentifier/NameIdentifier).
    /// No signature verification – just base64url decoding for tests.
    /// </summary>
    private static Guid? GetUserIdFromBearer(HttpClient client)
    {
        var auth = client.DefaultRequestHeaders.Authorization;
        if (auth is null || auth.Scheme != "Bearer" || string.IsNullOrWhiteSpace(auth.Parameter))
            return null;

        var token = auth.Parameter;
        var parts = token.Split('.');
        if (parts.Length < 2) return null;

        try
        {
            static byte[] B64Url(string s)
            {
                s = s.Replace('-', '+').Replace('_', '/');
                return Convert.FromBase64String(s.PadRight(s.Length + ((4 - s.Length % 4) % 4), '='));
            }

            var json = System.Text.Encoding.UTF8.GetString(B64Url(parts[1]));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Common claim names
            string[] candidates = new[] { "sub", "nameidentifier", "nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" };

            foreach (var c in candidates)
            {
                if (root.TryGetProperty(c, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    if (Guid.TryParse(v.GetString(), out var g)) return g;
                }
            }

            // Some tokens use "userId"
            if (root.TryGetProperty("userId", out var u) && u.ValueKind == JsonValueKind.String && Guid.TryParse(u.GetString(), out var g2))
                return g2;
        }
        catch
        {
            // ignore – return null and let caller throw a helpful error
        }
        return null;
    }

    private sealed class CreatedView
    {
        [JsonPropertyName("id")]
        public Guid id { get; set; }
    }
}
