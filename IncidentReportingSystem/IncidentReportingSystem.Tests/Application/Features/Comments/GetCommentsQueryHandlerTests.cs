using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;                
using IncidentReportingSystem.Application.Features.Comments.Dtos;       
using IncidentReportingSystem.Application.Features.Comments.Mappers;    
using IncidentReportingSystem.Application.Features.Comments.Queries.ListComment;
using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Tests.Application.Features.Comments
{
    /// <summary>Unit tests for GetCommentsQueryHandler (ordering and paging).</summary>
    public sealed class GetCommentsQueryHandlerTests
    {
        private sealed class FakeRepo : IIncidentCommentsRepository
        {
            public List<IncidentComment> Data = new();

            public Task<IncidentComment> AddAsync(IncidentComment comment, CancellationToken cancellationToken)
            { Data.Add(comment); return Task.FromResult(comment); }

            public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken cancellationToken)
                => Task.FromResult<IncidentComment?>(null);

            public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken cancellationToken)
                => Task.FromResult(true);

            public Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken cancellationToken)
            {
                var filtered = Data.FindAll(x => x.IncidentId == incidentId);
                filtered.Sort((a, b) => b.CreatedAtUtc.CompareTo(a.CreatedAtUtc));
                if (skip < 0) skip = 0;
                if (take <= 0) take = 50;
                var start = Math.Min(skip, filtered.Count);
                var length = Math.Max(0, Math.Min(take, filtered.Count - start));
                var slice = filtered.GetRange(start, length);
                return Task.FromResult<IReadOnlyList<IncidentComment>>(slice);
            }

            // NEW: required by interface — now used by the handler
            public Task<PagedResult<IncidentComment>> ListPagedAsync(Guid incidentId, int skip, int take, CancellationToken cancellationToken)
            {
                var filtered = Data.Where(x => x.IncidentId == incidentId).ToList();
                filtered.Sort((a, b) => b.CreatedAtUtc.CompareTo(a.CreatedAtUtc));

                if (skip < 0) skip = 0;
                if (take <= 0) take = 50;

                var items = filtered.Skip(skip).Take(take).ToList();
                return Task.FromResult(new PagedResult<IncidentComment>(items, total: filtered.Count, skip, take));
            }

            public Task RemoveAsync(IncidentComment comment, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        [Fact]
        public async Task Returns_Newest_First_With_Paging()
        {
            var repo = new FakeRepo();
            var incidentId = Guid.NewGuid();

            await repo.AddAsync(new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Guid.NewGuid(), Text = "c0", CreatedAtUtc = DateTime.UtcNow.AddSeconds(-3) }, CancellationToken.None);
            await repo.AddAsync(new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Guid.NewGuid(), Text = "c1", CreatedAtUtc = DateTime.UtcNow.AddSeconds(-2) }, CancellationToken.None);
            await repo.AddAsync(new IncidentComment { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Guid.NewGuid(), Text = "c2", CreatedAtUtc = DateTime.UtcNow.AddSeconds(-1) }, CancellationToken.None);

            var handler = new ListCommentsQueryHandler(repo);
            var page = await handler.Handle(new ListCommentsQuery(incidentId, Skip: 0, Take: 2), CancellationToken.None);

            Assert.Equal(2, page.Items.Count);
            Assert.Equal("c2", page.Items[0].Text);
            Assert.Equal("c1", page.Items[1].Text);
            Assert.Equal(3, page.Total);
            Assert.Equal(0, page.Skip);
            Assert.Equal(2, page.Take);
        }
    }
}
