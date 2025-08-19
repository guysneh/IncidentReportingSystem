using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;

namespace IncidentReportingSystem.Tests.Infrastructure
{
    public sealed class IncidentCommentsRepositoryTests
    {
        private static ApplicationDbContext NewContext()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            return new ApplicationDbContext(opts);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task IncidentExistsAsync_ReturnsTrue_WhenIncidentPresent()
        {
            await using var db = NewContext();
            var repo = new IncidentCommentsRepository(db);

            var incident = new IncidentReport
            (
                "desc",
                "loc",
                Guid.NewGuid(),
                Domain.Enums.IncidentCategory.Infrastructure,
                "API",
                0,
                DateTime.UtcNow
            );
            db.IncidentReports.Add(incident);
            await db.SaveChangesAsync();

            var exists = await repo.IncidentExistsAsync(incident.Id, default);
            exists.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Add_Get_Remove_Works()
        {
            await using var db = NewContext();
            var repo = new IncidentCommentsRepository(db);

            var incidentId = Guid.NewGuid();
            var incident = new IncidentReport
             (
                 "desc",
                 "loc",
                 Guid.NewGuid(),
                 Domain.Enums.IncidentCategory.Infrastructure,
                 "API",
                 0,
                 DateTime.UtcNow
             );
            await db.SaveChangesAsync();

            var c = new IncidentComment
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                UserId = Guid.NewGuid(),
                Text = "hello",
                CreatedAtUtc = DateTime.UtcNow
            };

            await repo.AddAsync(c, default);
            await db.SaveChangesAsync();

            var loaded = await repo.GetAsync(incidentId, c.Id, default);
            loaded.Should().NotBeNull();
            loaded!.Text.Should().Be("hello");

            await repo.RemoveAsync(loaded, default);
            await db.SaveChangesAsync();

            var gone = await repo.GetAsync(incidentId, c.Id, default);
            gone.Should().BeNull();
        }

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(0, -10)]
        [InlineData(1, 2)]
        [Trait("Category", "Integration")]
        public async Task ListAsync_FiltersAndOrdersNewestFirst(int skip, int take)
        {
            await using var db = NewContext();
            var repo = new IncidentCommentsRepository(db);

            var incidentA = Guid.NewGuid();
            var incidentB = Guid.NewGuid();

            // (אם יש FK ל-IncidentReports, ודא שה-Id תואם ל-incidentA/B)
            db.IncidentReports.AddRange(
                new IncidentReport ( "desc", "loc", Guid.NewGuid(), Domain.Enums.IncidentCategory.Infrastructure, "API", Domain.Enums.IncidentSeverity.Low, DateTime.UtcNow),
                new IncidentReport ( "desc", "loc", Guid.NewGuid(), Domain.Enums.IncidentCategory.Infrastructure, "API", Domain.Enums.IncidentSeverity.Low, DateTime.UtcNow)
            );

            var t0 = DateTime.UtcNow.AddSeconds(-3);
            var t1 = DateTime.UtcNow.AddSeconds(-2);
            var t2 = DateTime.UtcNow.AddSeconds(-1);

            db.IncidentComments.AddRange(
                new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentA, UserId = Guid.NewGuid(), Text = "c0", CreatedAtUtc = t0 },
                new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentA, UserId = Guid.NewGuid(), Text = "c1", CreatedAtUtc = t1 },
                new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentA, UserId = Guid.NewGuid(), Text = "c2", CreatedAtUtc = t2 },
                new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentB, UserId = Guid.NewGuid(), Text = "other", CreatedAtUtc = t2 }
            );
            await db.SaveChangesAsync();

            var list = await repo.ListAsync(incidentA, skip, take, default);

            list.Should().OnlyContain(x => x.IncidentId == incidentA);
            list.Select(x => x.Text).Should().BeInDescendingOrder(); // newest -> oldest

            var expectedFirst = skip > 0 ? "c1" : "c2";
            list.First().Text.Should().Be(expectedFirst);
        }

    }
}
