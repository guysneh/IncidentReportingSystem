using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Attachments
{
    /// <summary>
    /// E2E-style integration tests against your existing PostgreSQL defined in appsettings.Test.json.
    /// Data is created only via real use-cases (MediatR).
    /// </summary>
    public sealed class AttachmentRepositoryE2ETests : IClassFixture<TestAppFactory>
    {
        private readonly TestAppFactory _factory;
        private readonly TestSeeder _seeder;
        private readonly DatabaseCleaner _cleaner;

        public AttachmentRepositoryE2ETests(TestAppFactory factory)
        {
            _factory = factory;
            _cleaner = new DatabaseCleaner(_factory.Services);
            _seeder = new TestSeeder(_factory.Services);
        }

        [Fact]
        public async Task Search_By_FileName_CaseInsensitive_Works()
        {
            await _cleaner.TruncateAllAsync();

            var incidentId = await _seeder.CreateIncidentAsync();
            var id1 = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "Report-Alpha.PDF", "application/pdf", 100);
            await _seeder.CompleteAttachmentAsync(id1);
            var id2 = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "alphA-notes.txt", "text/plain", 20);
            await _seeder.CompleteAttachmentAsync(id2);
            var id3 = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "beta.png", "image/png", 50);
            await _seeder.CompleteAttachmentAsync(id3);

            using var scope = _factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
            var (items, total) = await repo.ListByParentAsync(
                AttachmentParentType.Incident, incidentId,
                new AttachmentListFilters(Search: "alpha", OrderBy: "fileName", Direction: "asc"),
                CancellationToken.None);

            total.Should().Be(2);
            items.Select(i => i.FileName).Should().ContainInOrder("alphA-notes.txt", "Report-Alpha.PDF");
        }

        [Fact]
        public async Task ContentType_And_DateRange_Inclusive()
        {
            await _cleaner.TruncateAllAsync();

            var incidentId = await _seeder.CreateIncidentAsync();
            var a1 = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "a.pdf", "application/pdf", 1);
            await _seeder.CompleteAttachmentAsync(a1);
            var a2 = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "b.pdf", "application/pdf", 2);
            await _seeder.CompleteAttachmentAsync(a2);
            var a3 = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "c.txt", "text/plain", 3);
            await _seeder.CompleteAttachmentAsync(a3);

            using var scope = _factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
            var (items, total) = await repo.ListByParentAsync(
                AttachmentParentType.Incident, incidentId,
                new AttachmentListFilters(
                    ContentType: "application/pdf",
                    CreatedAfter: DateTimeOffset.UtcNow.AddDays(-1),
                    CreatedBefore: DateTimeOffset.UtcNow.AddDays(1),
                    OrderBy: "createdAt",
                    Direction: "asc"),
                CancellationToken.None);

            total.Should().Be(2);
            items.Select(i => i.FileName).Should().ContainInOrder("a.pdf", "b.pdf");
        }

        [Fact]
        public async Task Sorting_By_Size_And_Paging()
        {
            await _cleaner.TruncateAllAsync();

            var incidentId = await _seeder.CreateIncidentAsync();
            var idA = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "a.bin", "application/octet-stream", 10);
            await _seeder.CompleteAttachmentAsync(idA);
            var idB = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "b.bin", "application/octet-stream", 30);
            await _seeder.CompleteAttachmentAsync(idB);
            var idC = await _seeder.StartAttachmentAsync(AttachmentParentType.Incident, incidentId, "c.bin", "application/octet-stream", 20);
            await _seeder.CompleteAttachmentAsync(idC);

            using var scope = _factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
            var (items, total) = await repo.ListByParentAsync(
                AttachmentParentType.Incident, incidentId,
                new AttachmentListFilters(OrderBy: "size", Direction: "desc", Skip: 1, Take: 1),
                CancellationToken.None);

            total.Should().Be(3);
            items.Should().HaveCount(1);
            items[0].FileName.Should().Be("c.bin");
        }

        [Fact]
        public async Task Works_For_Comment_Parent_Too()
        {
            await _cleaner.TruncateAllAsync();

            var incidentId = await _seeder.CreateIncidentAsync();
            var commentId = await _seeder.CreateCommentAsync(incidentId);

            var c1 = await _seeder.StartAttachmentAsync(AttachmentParentType.Comment, commentId, "com-1.txt", "text/plain", 1);
            await _seeder.CompleteAttachmentAsync(c1);

            using var scope = _factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
            var (items, total) = await repo.ListByParentAsync(
                AttachmentParentType.Comment, commentId,
                new AttachmentListFilters(OrderBy: "createdAt", Direction: "desc"),
                CancellationToken.None);

            total.Should().Be(1);
            items.Single().FileName.Should().Be("com-1.txt");
        }
    }
}
