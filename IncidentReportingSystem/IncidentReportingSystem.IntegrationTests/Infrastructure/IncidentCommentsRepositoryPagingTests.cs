using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure
{
    public class IncidentCommentsRepositoryPagingTests
    {
        private static ApplicationDbContext NewContext()
        {
            var opt = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(opt);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task NegativeSkip_TreatedAsZero_And_DefaultTake50()
        {
            await using var db = NewContext();
            var repo = new IncidentCommentsRepository(db);

            var inc = Guid.NewGuid();
            for (int i = 0; i < 3; i++)
            {
                db.IncidentComments.Add(new IncidentComment
                {
                    Id = Guid.NewGuid(),
                    IncidentId = inc,
                    UserId = Guid.NewGuid(),
                    Text = $"c{i}",
                    CreatedAtUtc = DateTime.UtcNow.AddSeconds(-i)
                });
            }
            await db.SaveChangesAsync();

            var list = await repo.ListAsync(inc, -10, 0, default);
            Assert.True(list.Count <= 50);
            Assert.Equal(new[] { "c0", "c1", "c2" }, list.Select(x => x.Text));
        }
    }
}
