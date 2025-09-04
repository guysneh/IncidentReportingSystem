using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Comments.Commands.Create;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Tests.Application.Features.Comments
{
    public sealed class CreateCommentCommandHandlerTests
    {
        // ---- Test doubles ----

        private sealed class FakeCommentsRepo : IIncidentCommentsRepository
        {
            public IncidentComment? Stored;

            public Task<IncidentComment> AddAsync(IncidentComment comment, CancellationToken cancellationToken)
            {
                Stored = comment;
                return Task.FromResult(comment);
            }

            public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken cancellationToken)
                => Task.FromResult(Stored is not null && Stored.IncidentId == incidentId && Stored.Id == commentId ? Stored : null);

            public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken cancellationToken)
                => Task.FromResult(Stored?.IncidentId == incidentId);

            public Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken cancellationToken)
                => Task.FromResult<IReadOnlyList<IncidentComment>>(Array.Empty<IncidentComment>());

            // NEW: required by interface
            public Task<PagedResult<IncidentComment>> ListPagedAsync(Guid incidentId, int skip, int take, CancellationToken cancellationToken)
            {
                var all = new List<IncidentComment>();
                if (Stored is not null && Stored.IncidentId == incidentId)
                    all.Add(Stored);

                if (skip < 0) skip = 0;
                if (take <= 0) take = 50;

                var items = all.Skip(skip).Take(take).ToList();
                return Task.FromResult(new PagedResult<IncidentComment>(items, all.Count, skip, take));
            }

            public Task RemoveAsync(IncidentComment comment, CancellationToken cancellationToken)
            { Stored = null; return Task.CompletedTask; }
        }

        private sealed class FakeIncidentReportRepo : IIncidentReportRepository
        {
            private readonly HashSet<Guid> _existing = new();
            public readonly Dictionary<Guid, DateTime> LastTouched = new();

            public void Seed(Guid id) => _existing.Add(id);

            public Task TouchModifiedAtAsync(Guid incidentId, DateTime utcNow, CancellationToken cancellationToken)
            {
                if (!_existing.Contains(incidentId))
                    throw new KeyNotFoundException($"Incident {incidentId} not found.");
                LastTouched[incidentId] = utcNow;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<IncidentReport>> GetAsync(IncidentStatus? status = null, int skip = 0, int take = 50, IncidentCategory? category = null, IncidentSeverity? severity = null, string? searchText = null, DateTime? reportedAfter = null, DateTime? reportedBefore = null, IncidentSortField sortBy = IncidentSortField.CreatedAt, SortDirection direction = SortDirection.Desc, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            // NEW: required by interface
            public Task<PagedResult<IncidentReport>> GetPagedAsync(
                IncidentStatus? status = null,
                int skip = 0,
                int take = 50,
                IncidentCategory? category = null,
                IncidentSeverity? severity = null,
                string? searchText = null,
                DateTime? reportedAfter = null,
                DateTime? reportedBefore = null,
                IncidentSortField sortBy = IncidentSortField.CreatedAt,
                SortDirection direction = SortDirection.Desc,
                CancellationToken cancellationToken = default)
            {
                var empty = Array.Empty<IncidentReport>();
                return Task.FromResult(new PagedResult<IncidentReport>(empty, total: 0, skip, take));
            }

            public Task<(int UpdatedCount, List<Guid> NotFound)> BulkUpdateStatusAsync(IReadOnlyList<Guid> ids, IncidentStatus newStatus, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<IncidentReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task<IReadOnlyList<IncidentReport>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task SaveAsync(IncidentReport report, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class FakeUow : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
        }

        // ---- Tests ----

        [Fact]
        public async Task Create_Adds_Comment_And_Touches_Incident()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo();
            var uow = new FakeUow();

            var incidentId = Guid.NewGuid();
            incidents.Seed(incidentId);

            var authorId = Guid.NewGuid();
            var cmd = new CreateCommentCommand(incidentId, authorId, "hello");

            var sut = new CreateCommentCommandHandler(comments, incidents, uow);

            var view = await sut.Handle(cmd, CancellationToken.None);

            Assert.NotNull(comments.Stored);
            Assert.Equal(incidentId, comments.Stored!.IncidentId);
            Assert.Equal(authorId, comments.Stored.UserId);
            Assert.Equal("hello", comments.Stored.Text);
            Assert.True(incidents.LastTouched.ContainsKey(incidentId));
            Assert.Equal(comments.Stored.CreatedAtUtc, incidents.LastTouched[incidentId], TimeSpan.FromSeconds(1));
            Assert.Equal(view.Id, comments.Stored.Id);
        }

        [Fact]
        public async Task Create_Uses_AuthorId_From_Command()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo();
            var uow = new FakeUow();

            var incidentId = Guid.NewGuid();
            incidents.Seed(incidentId);

            var author = Guid.NewGuid();
            var sut = new CreateCommentCommandHandler(comments, incidents, uow);

            await sut.Handle(new CreateCommentCommand(incidentId, author, "text"), CancellationToken.None);

            Assert.Equal(author, comments.Stored!.UserId);
        }

        [Fact]
        public async Task Missing_Incident_Throws_KeyNotFound()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo(); // not seeded => incident missing
            var uow = new FakeUow();

            var sut = new CreateCommentCommandHandler(comments, incidents, uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.Handle(new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "x"),
                           CancellationToken.None));
        }
    }
}
