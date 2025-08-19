using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Users;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Domain.Auth;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.IntegrationTests.Comments
{
    /// <summary>
    /// E2E tests for Comments API. Ensures users exist (by Id OR NormalizedEmail) to avoid unique violations.
    /// </summary>
    public sealed class CommentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private static readonly Guid TestUserId = new("ae751ab8-1418-44bc-b695-e81559fd4bfe");
        private static readonly Guid AdminUserId = new("b4e3a0c6-a4b6-4a77-a9db-4f2f2e5d40b1");

        private readonly CustomWebApplicationFactory _factory;
        public CommentsEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

        private async Task<Guid> CreateIncidentViaApiAsync(HttpClient client, Guid reporterId)
        {
            var payload = new
            {
                description = "Test incident",
                location = "Berlin",
                reporterId = reporterId,
                category = "ITSystems",
                systemAffected = "API",
                severity = "Medium",
                reportedAt = DateTime.UtcNow
            };

            var res = await client.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidentreports", payload);

            res.EnsureSuccessStatusCode();

            // If your API returns the created entity (recommended):
            var created = await res.Content.ReadFromJsonAsync<IncidentView>();
            if (created is null || created.Id == Guid.Empty)
                throw new InvalidOperationException("Incident creation via API did not return a valid Id.");

            return created.Id;
        }

        private sealed class IncidentView
        {
            public Guid Id { get; set; }
        }


        private static async Task EnsureUserExistsAsync(IServiceProvider services, Guid id, string email, string role)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var normalized = email.ToUpperInvariant();

            var repo = sp.GetService<IUserRepository>();
            if (repo is not null)
            {
                var existsById = await repo.ExistsByIdAsync(id, default);
                var existsByEmail = await repo.ExistsByNormalizedEmailAsync(normalized, default);
                if (!existsById && !existsByEmail)
                {
                    var u = new User
                    {
                        Id = id,
                        Email = email,
                        NormalizedEmail = normalized
                    };
                    u.SetRoles(new[] { role });
                    await repo.AddAsync(u, default);
                }
                return;
            }

            var db = sp.GetRequiredService<ApplicationDbContext>();
            var present = db.Users.Any(u => u.Id == id || u.NormalizedEmail == normalized);
            if (!present)
            {
                var u = new User
                {
                    Id = id,
                    Email = email,
                    NormalizedEmail = normalized
                };
                u.SetRoles(new[] { role });
                db.Users.Add(u);
                await db.SaveChangesAsync();
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_List_Delete_Owner_Succeeds()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: TestUserId,
                roles: new[] { Roles.User },
                email: "user@example.com");

            await EnsureUserExistsAsync(_factory.Services, TestUserId, "user@example.com", Roles.User);

            var incidentId = await CreateIncidentViaApiAsync(client, TestUserId);
            var probe = await client.GetAsync($"api/{_factory.ApiVersionSegment}/incidentreports/{incidentId}");
            if (probe.StatusCode == HttpStatusCode.NotFound)
                throw new InvalidOperationException("Sanity check: API cannot see the incident created via DbContext — indicates split DBs in CI.");

            var create = await client.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments",
                new { text = "First" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var list = await client.GetFromJsonAsync<List<CommentView>>(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments?skip=0&take=10");
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
            var commentId = list![0].Id;

            var del = await client.DeleteAsync($"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments/{commentId}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_EmptyText_BadRequest()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: TestUserId,
                roles: new[] { Roles.User },
                email: "user@example.com");

            await EnsureUserExistsAsync(_factory.Services, TestUserId, "user@example.com", Roles.User);
            var incidentId = await CreateIncidentViaApiAsync(client, TestUserId);
            var probe = await client.GetAsync($"api/{_factory.ApiVersionSegment}/incidentreports/{incidentId}");
            if (probe.StatusCode == HttpStatusCode.NotFound)
                throw new InvalidOperationException("Sanity check: API cannot see the incident created via DbContext — indicates split DBs in CI.");

            var res = await client.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments",
                new { text = "" });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_OnMissingIncident_NotFound()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: TestUserId,
                roles: new[] { Roles.User },
                email: "user@example.com");

            await EnsureUserExistsAsync(_factory.Services, TestUserId, "user@example.com", Roles.User);

            var res = await client.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidents/{Guid.NewGuid()}/comments",
                new { text = "Nope" });
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task List_Is_Newest_First_With_Paging()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: TestUserId,
                roles: new[] { Roles.User },
                email: "user@example.com");

            await EnsureUserExistsAsync(_factory.Services, TestUserId, "user@example.com", Roles.User);

            var incidentId = await CreateIncidentViaApiAsync(client, TestUserId);
            var probe = await client.GetAsync($"api/{_factory.ApiVersionSegment}/incidentreports/{incidentId}");
            if (probe.StatusCode == HttpStatusCode.NotFound)
                throw new InvalidOperationException("Sanity check: API cannot see the incident created via DbContext — indicates split DBs in CI.");
            for (var i = 0; i < 3; i++)
            {
                var r = await client.PostAsJsonAsync(
                    $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments",
                    new { text = $"c{i}" });
                r.EnsureSuccessStatusCode();
                await Task.Delay(25);
            }
            var list = await client.GetFromJsonAsync<List<CommentView>>(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments?skip=0&take=2");
            Assert.Equal(2, list!.Count);
            Assert.Equal("c2", list[0].Text);
            Assert.Equal("c1", list[1].Text);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Delete_By_Stranger_Forbidden()
        {
            var ownerId = TestUserId;
            var owner = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: ownerId,
                roles: new[] { Roles.User },
                email: "owner@example.com");
            await EnsureUserExistsAsync(_factory.Services, ownerId, "owner@example.com", Roles.User);

            var strangerId = new Guid("c4f296d9-3c29-44c0-8f18-060366db2f0a");
            var stranger = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: strangerId,
                roles: new[] { Roles.User },
                email: "stranger@example.com");
            await EnsureUserExistsAsync(_factory.Services, strangerId, "stranger@example.com", Roles.User);

            var incidentId = await CreateIncidentViaApiAsync(owner, TestUserId);
            var probe = await stranger.GetAsync($"api/{_factory.ApiVersionSegment}/incidentreports/{incidentId}");
            if (probe.StatusCode == HttpStatusCode.NotFound)
                throw new InvalidOperationException("Sanity check: API cannot see the incident created via DbContext — indicates split DBs in CI.");

            var create = await owner.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments",
                new { text = "private" });
            create.EnsureSuccessStatusCode();

            var list = await owner.GetFromJsonAsync<List<CommentView>>(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments");
            var commentId = list![0].Id;

            var del = await stranger.DeleteAsync($"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments/{commentId}");
            Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Delete_By_Admin_Succeeds()
        {
            var ownerId = TestUserId;
            var owner = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: ownerId,
                roles: new[] { Roles.User },
                email: "owner@example.com");
            await EnsureUserExistsAsync(_factory.Services, ownerId, "owner@example.com", Roles.User);

            var admin = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory,
                userId: AdminUserId,
                roles: new[] { Roles.Admin },
                email: "admin@example.com");
            await EnsureUserExistsAsync(_factory.Services, AdminUserId, "admin@example.com", Roles.Admin);

            var incidentId = await CreateIncidentViaApiAsync(owner, TestUserId);
            var probe = await owner.GetAsync($"api/{_factory.ApiVersionSegment}/incidentreports/{incidentId}");
            if (probe.StatusCode == HttpStatusCode.NotFound)
                throw new InvalidOperationException("Sanity check: API cannot see the incident created via DbContext — indicates split DBs in CI.");

            var create = await owner.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments",
                new { text = "moderate" });
            create.EnsureSuccessStatusCode();

            var list = await owner.GetFromJsonAsync<List<CommentView>>(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments");
            var commentId = list![0].Id;

            var del = await admin.DeleteAsync($"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments/{commentId}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_Unauthenticated_Unauthorized()
        {
            // Seed via API with an authenticated owner
            var ownerId = Guid.NewGuid();
            var owner = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: ownerId, roles: new[] { Roles.User }, email: "user@example.com");
            await EnsureUserExistsAsync(_factory.Services, ownerId, "user@example.com", Roles.User);

            var incidentId = await CreateIncidentViaApiAsync(owner, ownerId);

            // Now perform the action under test with an anonymous client
            var anonymous = _factory.AsAnonymous();
            var res = await anonymous.PostAsJsonAsync(
                $"api/{_factory.ApiVersionSegment}/incidents/{incidentId}/comments",
                new { text = "no token" });

            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        private sealed class CommentView
        {
            public Guid Id { get; set; }
            public Guid IncidentId { get; set; }
            public Guid UserId { get; set; }
            public string Text { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
