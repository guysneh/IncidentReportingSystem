using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure.Persistence.Repositories
{
    [Trait("Category", "Integration")]
    public sealed class UserRepositoryTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UserRepositoryTests(CustomWebApplicationFactory factory) => _factory = factory;

        private (ApplicationDbContext Db, UserRepository Repo) GetRepo()
        {
            var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (db, new UserRepository(db));
        }

        private static User NewUser(string email)
        {
            return new User()
            {
                Email = email,
                NormalizedEmail = email.ToUpperInvariant()
            };
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Exists_And_Find_By_NormalizedEmail_True_And_False()
        {
            var (db, repo) = GetRepo();
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            var u = NewUser("alice@test.local");
            await repo.AddAsync(u, CancellationToken.None);
            await db.SaveChangesAsync();

            (await repo.ExistsByNormalizedEmailAsync(u.NormalizedEmail!, CancellationToken.None)).Should().BeTrue();
            (await repo.ExistsByNormalizedEmailAsync("NOT@EXIST", CancellationToken.None)).Should().BeFalse();

            var found = await repo.FindByNormalizedEmailAsync(u.NormalizedEmail!, CancellationToken.None);
            found.Should().NotBeNull();
            var notFound = await repo.FindByNormalizedEmailAsync("MISSING", CancellationToken.None);
            notFound.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ExistsById_True_And_False()
        {
            var (db, repo) = GetRepo();
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();

            var u = NewUser("bob@test.local");
            await repo.AddAsync(u, CancellationToken.None);
            await db.SaveChangesAsync();

            (await repo.ExistsByIdAsync(u.Id, CancellationToken.None)).Should().BeTrue();
            (await repo.ExistsByIdAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeFalse();
        }
    }
}
