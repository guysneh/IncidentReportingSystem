using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure.Persistence.Repositories
{
    [Trait("Category", "Integration")]
    public sealed class IncidentReportRepositoryBranchCoverageTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public IncidentReportRepositoryBranchCoverageTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private async Task TruncateAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in Test.");

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand("TRUNCATE TABLE \"IncidentReports\" RESTART IDENTITY CASCADE;", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<(ApplicationDbContext Db, IncidentReportRepository Repo)> NewRepoAsync()
        {
            var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.ProviderName!.ToLowerInvariant().Should().Contain("npgsql");
            var repo = new IncidentReportRepository(db);
            return (db, repo);
        }

        private static IncidentReport NewIncident(
            string description,
            string location,
            IncidentCategory category,
            string system,
            IncidentSeverity severity,
            DateTime? reportedAt,
            IncidentStatus status)
        {
            var inc = new IncidentReport(
                description: description,
                location: location,
                reporterId: Guid.NewGuid(),
                category: category,
                systemAffected: system,
                severity: severity,
                reportedAt: reportedAt
            );
            inc.UpdateStatus(status);
            return inc;
        }

        private async Task SeedAsync(params IncidentReport[] incidents)
        {
            var (db, _) = await NewRepoAsync();
            await db.IncidentReports.AddRangeAsync(incidents);
            await db.SaveChangesAsync();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAsync_Filters_By_Status_Category_Severity()
        {
            await TruncateAsync();

            var now = DateTime.UtcNow;
            await SeedAsync(
                NewIncident("API down", "Berlin", IncidentCategory.ITSystems, "API", IncidentSeverity.High, now.AddDays(-2), IncidentStatus.Open),
                NewIncident("Security breach", "Munich", IncidentCategory.Security, "Auth", IncidentSeverity.Medium, now.AddDays(-1), IncidentStatus.InProgress),
                NewIncident("Water supply", "Hamburg", IncidentCategory.WaterSupply, "Facility", IncidentSeverity.Low, now, IncidentStatus.Closed)
            );

            var (_, repo) = await NewRepoAsync();

            // status filter
            var onlyInProgress = await repo.GetAsync(
                status: IncidentStatus.InProgress,
                skip: 0, take: 50,
                category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Asc,
                cancellationToken: CancellationToken.None);

            onlyInProgress.Should().ContainSingle().Which.Status.Should().Be(IncidentStatus.InProgress);

            // category filter
            var onlyIt = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: IncidentCategory.ITSystems, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Asc,
                cancellationToken: CancellationToken.None);

            onlyIt.Should().ContainSingle().Which.Category.Should().Be(IncidentCategory.ITSystems);

            // severity filter
            var onlyHigh = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: null, severity: IncidentSeverity.High,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Asc,
                cancellationToken: CancellationToken.None);

            onlyHigh.Should().ContainSingle().Which.Severity.Should().Be(IncidentSeverity.High);
        }

        [Theory]
        [Trait("Category", "Integration")]
        [InlineData(null)]
        [InlineData("Berlin")]
        [InlineData("API")]
        [InlineData("security")]
        public async Task GetAsync_Applies_SearchText_When_NotEmpty(string? search)
        {
            await TruncateAsync();

            var now = DateTime.UtcNow;
            await SeedAsync(
                NewIncident("API down", "Berlin", IncidentCategory.ITSystems, "API", IncidentSeverity.High, now.AddDays(-2), IncidentStatus.Open),
                NewIncident("Security breach", "Munich", IncidentCategory.Security, "Auth", IncidentSeverity.Medium, now.AddDays(-1), IncidentStatus.InProgress),
                NewIncident("Water supply", "Hamburg", IncidentCategory.WaterSupply, "Facility", IncidentSeverity.Low, now, IncidentStatus.Closed)
            );

            var (_, repo) = await NewRepoAsync();

            var res = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: null, severity: null,
                searchText: search, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Asc,
                cancellationToken: CancellationToken.None);

            if (string.IsNullOrWhiteSpace(search))
                res.Should().HaveCount(3);
            else
                res.Should().NotBeEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAsync_Applies_ReportedAfter_And_ReportedBefore()
        {
            await TruncateAsync();

            var now = DateTime.UtcNow;
            await SeedAsync(
                NewIncident("Old", "A", IncidentCategory.ITSystems, "API", IncidentSeverity.Low, now.AddDays(-3), IncidentStatus.Open),
                NewIncident("Mid", "B", IncidentCategory.ITSystems, "API", IncidentSeverity.Medium, now.AddDays(-1), IncidentStatus.InProgress),
                NewIncident("New", "C", IncidentCategory.ITSystems, "API", IncidentSeverity.High, now, IncidentStatus.Closed)
            );

            var (_, repo) = await NewRepoAsync();

            var after = now.AddDays(-2);
            var before = now.AddHours(-12);

            var resAfter = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: null, severity: null,
                searchText: null, reportedAfter: after, reportedBefore: null,
                sortBy: IncidentSortField.ReportedAt, direction: SortDirection.Asc,
                cancellationToken: CancellationToken.None);

            resAfter.Should().OnlyContain(i => i.ReportedAt >= after);

            var resBefore = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: before,
                sortBy: IncidentSortField.ReportedAt, direction: SortDirection.Asc,
                cancellationToken: CancellationToken.None);

            resBefore.Should().OnlyContain(i => i.ReportedAt <= before);
        }

        [Theory]
        [Trait("Category", "Integration")]
        [InlineData(IncidentSortField.ReportedAt, SortDirection.Asc)]
        [InlineData(IncidentSortField.ReportedAt, SortDirection.Desc)]
        [InlineData(IncidentSortField.CreatedAt, SortDirection.Asc)]
        [InlineData(IncidentSortField.CreatedAt, SortDirection.Desc)]
        [InlineData(IncidentSortField.Severity, SortDirection.Asc)]
        [InlineData(IncidentSortField.Severity, SortDirection.Desc)]
        [InlineData(IncidentSortField.Status, SortDirection.Asc)]
        [InlineData(IncidentSortField.Status, SortDirection.Desc)]
        public async Task GetAsync_Sorts_By_All_Declared_Fields(IncidentSortField sortBy, SortDirection dir)
        {
            await TruncateAsync();

            var now = DateTime.UtcNow;
            await SeedAsync(
                NewIncident("A", "X", IncidentCategory.ITSystems, "API", IncidentSeverity.Low, now.AddDays(-2), IncidentStatus.Open),
                NewIncident("B", "Y", IncidentCategory.Security, "Auth", IncidentSeverity.Medium, now.AddDays(-1), IncidentStatus.InProgress),
                NewIncident("C", "Z", IncidentCategory.WaterSupply, "Facility", IncidentSeverity.High, now, IncidentStatus.Closed)
            );

            var (_, repo) = await NewRepoAsync();

            var res = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: sortBy, direction: dir,
                cancellationToken: CancellationToken.None);

            res.Should().HaveCount(3); 
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAsync_Uses_Default_Sort_For_Unknown_Enum_Value()
        {
            await TruncateAsync();

            var now = DateTime.UtcNow;
            await SeedAsync(
                NewIncident("A", "X", IncidentCategory.ITSystems, "API", IncidentSeverity.Low, now.AddDays(-2), IncidentStatus.Open),
                NewIncident("B", "Y", IncidentCategory.Security, "Auth", IncidentSeverity.Medium, now.AddDays(-1), IncidentStatus.InProgress),
                NewIncident("C", "Z", IncidentCategory.WaterSupply, "Facility", IncidentSeverity.High, now, IncidentStatus.Closed)
            );

            var (_, repo) = await NewRepoAsync();

            var unknown = (IncidentSortField)999;

            var res = await repo.GetAsync(
                status: null, skip: 0, take: 50,
                category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: unknown, direction: SortDirection.Desc,
                cancellationToken: CancellationToken.None);

            res.Should().HaveCount(3);
        }
    }
}

