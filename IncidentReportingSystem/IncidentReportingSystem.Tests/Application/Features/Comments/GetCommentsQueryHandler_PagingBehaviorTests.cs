using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Comments.Dtos;
using IncidentReportingSystem.Application.Features.Comments.Mappers;
using IncidentReportingSystem.Application.Features.Comments.Queries.ListComment;
using IncidentReportingSystem.Domain.Entities;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.Features.Comments
{
    /// <summary>
    /// Verifies handler paging behavior (empty slice when skip >= total).
    /// </summary>
    public sealed class GetCommentsQueryHandler_PagingBehaviorTests
    {
        private sealed class FakeRepo : IIncidentCommentsRepository
        {
            public readonly List<IncidentComment> Data = new();

            public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken ct) => Task.FromResult(true);
            public Task<IncidentComment> AddAsync(IncidentComment c, CancellationToken ct) { Data.Add(c); return Task.FromResult(c); }
            public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken ct) => Task.FromResult<IncidentComment?>(null);
            public Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken ct)
                => Task.FromResult<IReadOnlyList<IncidentComment>>(Array.Empty<IncidentComment>());

            public Task<PagedResult<IncidentComment>> ListPagedAsync(Guid incidentId, int skip, int take, CancellationToken ct)
            {
                var filtered = Data.Where(d => d.IncidentId == incidentId).OrderByDescending(d => d.CreatedAtUtc).ToList();
                var total = filtered.Count;
                if (skip < 0) skip = 0;
                if (take <= 0) take = 50;
                var items = filtered.Skip(skip).Take(take).ToList();
                return Task.FromResult(new PagedResult<IncidentComment>(items, total, skip, take));
            }

            public Task RemoveAsync(IncidentComment c, CancellationToken ct) => Task.CompletedTask;
        }

        [Fact]
        public async Task Skip_Beyond_Total_Should_Return_Empty_Items()
        {
            var repo = new FakeRepo();
            var incidentId = Guid.NewGuid();

            // Seed 3 comments
            await repo.AddAsync(new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Guid.NewGuid(), Text = "c1", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3) }, CancellationToken.None);
            await repo.AddAsync(new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Guid.NewGuid(), Text = "c2", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2) }, CancellationToken.None);
            await repo.AddAsync(new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Guid.NewGuid(), Text = "c3", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1) }, CancellationToken.None);

            var handler = new ListCommentsQueryHandler(repo);
            var page = await handler.Handle(new ListCommentsQuery(incidentId, Skip: 10, Take: 5), CancellationToken.None);

            page.Total.Should().Be(3);
            page.Skip.Should().Be(10);
            page.Take.Should().Be(5);
            page.Items.Should().BeEmpty();
        }
    }
}
