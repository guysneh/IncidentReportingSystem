using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Features.Comments.Commands.Delete;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Tests.Application.Features.Comments
{
    public sealed class DeleteCommentCommandHandlerTests
    {
        private sealed class FakeCommentsRepo : IIncidentCommentsRepository
        {
            public IncidentComment? Stored;

            public Task<IncidentComment> AddAsync(IncidentComment comment, CancellationToken ct)
            { Stored = comment; return Task.FromResult(comment); }

            public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken ct)
                => Task.FromResult(Stored is not null && Stored.IncidentId == incidentId && Stored.Id == commentId ? Stored : null);

            public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken ct)
                => Task.FromResult(Stored?.IncidentId == incidentId);

            public Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken ct)
                => Task.FromResult<IReadOnlyList<IncidentComment>>(Array.Empty<IncidentComment>());

            public Task RemoveAsync(IncidentComment comment, CancellationToken ct)
            { Stored = null; return Task.CompletedTask; }
        }

        private sealed class FakeIncidentReportRepo : IIncidentReportRepository
        {
            private readonly HashSet<Guid> _existing = new();
            public readonly Dictionary<Guid, DateTime> LastTouched = new();

            public void Seed(Guid id) => _existing.Add(id);

            public Task TouchModifiedAtAsync(Guid incidentId, DateTime utcNow, CancellationToken ct)
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

            public Task<(int UpdatedCount, List<Guid> NotFound)> BulkUpdateStatusAsync(IReadOnlyList<Guid> ids, IncidentStatus newStatus, CancellationToken ct)
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
            public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
        }

        [Fact]
        public async Task Owner_Can_Delete_And_Touches_Incident()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo();
            var uow = new FakeUow();

            var owner = Guid.NewGuid();
            var c = new IncidentComment { Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), UserId = owner, Text = "x", CreatedAtUtc = DateTime.UtcNow };
            await comments.AddAsync(c, CancellationToken.None);
            incidents.Seed(c.IncidentId);

            var sut = new DeleteCommentCommandHandler(comments, incidents, uow);

            await sut.Handle(new DeleteCommentCommand(c.IncidentId, c.Id, owner, false), CancellationToken.None);

            Assert.Null(comments.Stored);
            Assert.True(incidents.LastTouched.ContainsKey(c.IncidentId));
        }

        [Fact]
        public async Task Admin_Can_Delete_And_Touches_Incident()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo();
            var uow = new FakeUow();

            var owner = Guid.NewGuid();
            var admin = Guid.NewGuid();

            var c = new IncidentComment { Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), UserId = owner, Text = "x", CreatedAtUtc = DateTime.UtcNow };
            await comments.AddAsync(c, CancellationToken.None);
            incidents.Seed(c.IncidentId);

            var sut = new DeleteCommentCommandHandler(comments, incidents, uow);

            await sut.Handle(new DeleteCommentCommand(c.IncidentId, c.Id, admin, true), CancellationToken.None);

            Assert.Null(comments.Stored);
            Assert.True(incidents.LastTouched.ContainsKey(c.IncidentId));
        }

        [Fact]
        public async Task Stranger_Cannot_Delete_Forbidden()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo();
            var uow = new FakeUow();

            var owner = Guid.NewGuid();
            var stranger = Guid.NewGuid();

            var c = new IncidentComment { Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), UserId = owner, Text = "x", CreatedAtUtc = DateTime.UtcNow };
            await comments.AddAsync(c, CancellationToken.None);
            incidents.Seed(c.IncidentId);

            var sut = new DeleteCommentCommandHandler(comments, incidents, uow);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                sut.Handle(new DeleteCommentCommand(c.IncidentId, c.Id, stranger, false), CancellationToken.None));

            Assert.NotNull(comments.Stored);
            Assert.False(incidents.LastTouched.ContainsKey(c.IncidentId));
        }

        [Fact]
        public async Task Missing_Comment_Throws_KeyNotFound()
        {
            var comments = new FakeCommentsRepo();
            var incidents = new FakeIncidentReportRepo();
            var uow = new FakeUow();
            incidents.Seed(Guid.NewGuid());

            var sut = new DeleteCommentCommandHandler(comments, incidents, uow);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                sut.Handle(new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None));
        }
    }
}
