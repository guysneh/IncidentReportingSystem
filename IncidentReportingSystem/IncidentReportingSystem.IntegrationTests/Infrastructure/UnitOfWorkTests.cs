using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Infrastructure.Persistence;

namespace IncidentReportingSystem.Tests.Infrastructure
{
    public sealed class UnitOfWorkTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task SaveChangesAsync_Commits()
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("uow-db")
                .Options;
            await using var db = new ApplicationDbContext(opts);
            var uow = new UnitOfWork(db);

            var before = await uow.SaveChangesAsync(default);
            before.Should().Be(0);
        }
    }
}
