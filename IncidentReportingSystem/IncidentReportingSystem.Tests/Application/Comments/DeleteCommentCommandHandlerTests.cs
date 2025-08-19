using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Comments.Handlers;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;

namespace IncidentReportingSystem.Tests.Unit.Application.Comments
{
    /// <summary>Unit tests for DeleteCommentCommandHandler business rules.</summary>
    public sealed class DeleteCommentCommandHandlerTests
    {
        private sealed class FakeRepo : IIncidentCommentsRepository
        {
            public IncidentComment? Stored;
            public Task<IncidentComment> AddAsync(IncidentComment comment, CancellationToken ct) { Stored = comment; return Task.FromResult(comment); }
            public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken ct)
                => Task.FromResult(Stored is not null && Stored.IncidentId == incidentId && Stored.Id == commentId ? Stored : null);
            public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken ct) => Task.FromResult(true);
            public Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken ct)
                => Task.FromResult<IReadOnlyList<IncidentComment>>(Array.Empty<IncidentComment>());
            public Task RemoveAsync(IncidentComment comment, CancellationToken ct) { Stored = null; return Task.CompletedTask; }
        }

        private sealed class FakeUow : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
        }

        [Fact]
        public async Task Owner_Can_Delete()
        {
            var repo = new FakeRepo();
            var ownerId = Guid.NewGuid();
            var c = new IncidentComment { Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), UserId = ownerId, Text = "x", CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(c, CancellationToken.None);

            var handler = new DeleteCommentCommandHandler(repo, new FakeUow());
            await handler.Handle(new DeleteCommentCommand(c.IncidentId, c.Id, ownerId, false), CancellationToken.None);

            Assert.Null(repo.Stored);
        }

        [Fact]
        public async Task Admin_Can_Delete()
        {
            var repo = new FakeRepo();
            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var c = new IncidentComment { Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), UserId = ownerId, Text = "x", CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(c, CancellationToken.None);

            var handler = new DeleteCommentCommandHandler(repo, new FakeUow());
            await handler.Handle(new DeleteCommentCommand(c.IncidentId, c.Id, adminId, true), CancellationToken.None);

            Assert.Null(repo.Stored);
        }

        [Fact]
        public async Task Stranger_Cannot_Delete()
        {
            var repo = new FakeRepo();
            var ownerId = Guid.NewGuid();
            var strangerId = Guid.NewGuid();
            var c = new IncidentComment { Id = Guid.NewGuid(), IncidentId = Guid.NewGuid(), UserId = ownerId, Text = "x", CreatedAtUtc = DateTime.UtcNow };
            await repo.AddAsync(c, CancellationToken.None);

            var handler = new DeleteCommentCommandHandler(repo, new FakeUow());

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(new DeleteCommentCommand(c.IncidentId, c.Id, strangerId, false), CancellationToken.None));
        }

        [Fact]
        public async Task NotFound_Throws_404()
        {
            var repo = new FakeRepo();
            var handler = new DeleteCommentCommandHandler(repo, new FakeUow());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), false), CancellationToken.None));
        }
    }
}
