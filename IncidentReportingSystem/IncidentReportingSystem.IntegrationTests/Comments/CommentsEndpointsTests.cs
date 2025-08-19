using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Users;          
using IncidentReportingSystem.Domain.Auth;           
using IncidentReportingSystem.IntegrationTests.Utils; 

namespace IncidentReportingSystem.IntegrationTests.Comments
{
    /// <summary>
    /// End-to-end tests for the Comments HTTP API (real JWT + Postgres via factory).
    /// </summary>
    public sealed class CommentsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
    {
        // Use fixed IDs to make debugging and DB cleanup easy
        private static readonly Guid OwnerId = new("ae751ab8-1418-44bc-b695-e81559fd4bfe");
        private static readonly Guid AdminId = Guid.NewGuid();
        private static readonly Guid StrangerId = Guid.NewGuid();

        private readonly CustomWebApplicationFactory _factory;
        public CommentsEndpointsTests(CustomWebApplicationFactory factory) => _factory = factory;

        #region Helpers

        private static string Base(string apiVersion, Guid incidentId) =>
            $"api/{apiVersion}/incidents/{incidentId}/comments";

        private async Task<Guid> CreateIncidentAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Build the incident using the domain constructor (properties are read-only).
            var incident = new IncidentReport(
                description: "Test incident",
                location: "Berlin",
                reporterId: Guid.NewGuid(),
                category: IncidentCategory.ITSystems,   // use your existing enum value
                systemAffected: "API",
                severity: IncidentSeverity.Medium,      // use your existing enum value
                reportedAt: DateTime.UtcNow
            );

            db.IncidentReports.Add(incident);
            await db.SaveChangesAsync();

            return incident.Id;
        }

        /// <summary>
        /// Inserts a minimal user row if missing (email + normalized email + roles).
        /// </summary>
        private static async Task EnsureUserAsync(IServiceProvider sp, Guid id, string email, string normalizedEmail, string role)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!db.Users.Any(u => u.Id == id))
            {
                var user = new User
                {
                    Id = id,
                    Email = email,
                    NormalizedEmail = normalizedEmail
                };
                // your User entity exposes SetRoles in the domain model
                user.SetRoles(new[] { role });

                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
        }

        private sealed class CommentView
        {
            public Guid Id { get; set; }
            public Guid IncidentId { get; set; }
            public Guid UserId { get; set; }
            public string Text { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
        }

        #endregion

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Delete_By_Author_Succeeds()
        {
            // Arrange (owner + incident)
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "owner@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "owner@example.com", "OWNER@EXAMPLE.COM", Roles.User);

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            // Create
            var create = await client.PostAsJsonAsync(
                urlBase, new { text = "by-owner" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            // Get list & take the created comment id
            var list = await client.GetFromJsonAsync<List<CommentView>>(urlBase + "?skip=0&take=10");
            Assert.NotNull(list);
            var commentId = list![0].Id;

            // Act: delete by the same owner
            var del = await client.DeleteAsync($"{urlBase}/{commentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            // Optional: ensure it's gone
            var listAfter = await client.GetFromJsonAsync<List<CommentView>>(urlBase);
            Assert.True(listAfter!.All(c => c.Id != commentId));
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_List_Delete_Owner_FullFlow_Succeeds()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "user@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "user@example.com", "USER@EXAMPLE.COM", Roles.User);

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            // Create
            var create = await client.PostAsJsonAsync(urlBase, new { text = "First" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            // List
            var list = await client.GetFromJsonAsync<List<CommentView>>(urlBase + "?skip=0&take=10");
            Assert.NotNull(list);
            Assert.True(list!.Count >= 1);
            var commentId = list[0].Id;

            // Delete (owner)
            var del = await client.DeleteAsync($"{urlBase}/{commentId}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_EmptyText_BadRequest()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "user@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "user@example.com", "USER@EXAMPLE.COM", Roles.User);

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            var res = await client.PostAsJsonAsync(urlBase, new { text = "" });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_OnMissingIncident_NotFound()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "user@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "user@example.com", "USER@EXAMPLE.COM", Roles.User);

            var urlBase = $"api/{TestConstants.ApiVersion}/incidents/{Guid.NewGuid()}/comments";
            var res = await client.PostAsJsonAsync(urlBase, new { text = "Nope" });
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task List_Is_Newest_First_With_Paging()
        {
            var client = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "user@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "user@example.com", "USER@EXAMPLE.COM", Roles.User);

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            for (var i = 0; i < 3; i++)
            {
                var r = await client.PostAsJsonAsync(urlBase, new { text = $"c{i}" });
                r.EnsureSuccessStatusCode();
                await Task.Delay(25);
            }

            var list = await client.GetFromJsonAsync<List<CommentView>>(urlBase + "?skip=0&take=2");
            Assert.Equal(2, list!.Count);
            Assert.Equal("c2", list[0].Text);
            Assert.Equal("c1", list[1].Text);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Delete_By_Stranger_Forbidden()
        {
            // Owner
            var owner = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "owner@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "owner@example.com", "OWNER@EXAMPLE.COM", Roles.User);

            // Stranger
            var stranger = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: StrangerId, roles: new[] { Roles.User }, email: "stranger@example.com");
            await EnsureUserAsync(_factory.Services, StrangerId, "stranger@example.com", "STRANGER@EXAMPLE.COM", Roles.User);

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            var create = await owner.PostAsJsonAsync(urlBase, new { text = "private" });
            create.EnsureSuccessStatusCode();

            var list = await owner.GetFromJsonAsync<List<CommentView>>(urlBase);
            var commentId = list![0].Id;

            var del = await stranger.DeleteAsync($"{urlBase}/{commentId}");
            Assert.Equal(HttpStatusCode.Forbidden, del.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Delete_By_Admin_Succeeds()
        {
            var owner = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: OwnerId, roles: new[] { Roles.User }, email: "owner@example.com");
            await EnsureUserAsync(_factory.Services, OwnerId, "owner@example.com", "OWNER@EXAMPLE.COM", Roles.User);

            var admin = AuthenticatedHttpClientFactory.CreateClientWithToken(
                _factory, userId: AdminId, roles: new[] { Roles.Admin }, email: "admin@example.com");
            await EnsureUserAsync(_factory.Services, AdminId, "admin@example.com", "ADMIN@EXAMPLE.COM", Roles.Admin);

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            var create = await owner.PostAsJsonAsync(urlBase, new { text = "moderate" });
            create.EnsureSuccessStatusCode();

            var list = await owner.GetFromJsonAsync<List<CommentView>>(urlBase);
            var commentId = list![0].Id;

            var del = await admin.DeleteAsync($"{urlBase}/{commentId}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Create_Unauthenticated_Unauthorized()
        {
            var client = _factory.AsAnonymous();

            var incidentId = await CreateIncidentAsync();
            var urlBase = Base(TestConstants.ApiVersion, incidentId);

            var res = await client.PostAsJsonAsync(urlBase, new { text = "no token" });
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }
    }
}
