using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Comments.Handlers;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using Moq;

namespace IncidentReportingSystem.Tests.Application.Comments
{
    /// <summary>Unit tests for CreateCommentCommandHandler.</summary>
    public sealed class CreateCommentCommandHandlerTests
    {
        // Minimal in-memory repo for comments
        private sealed class FakeCommentsRepo : IIncidentCommentsRepository
        {
            public bool IncidentExists = true;
            public IncidentComment? Added;

            public Task<IncidentComment> AddAsync(IncidentComment comment, CancellationToken ct)
            { Added = comment; return Task.FromResult(comment); }

            public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken ct)
                => Task.FromResult<IncidentComment?>(null);

            public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken ct)
                => Task.FromResult(IncidentExists);

            public Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken ct)
                => Task.FromResult<IReadOnlyList<IncidentComment>>(Array.Empty<IncidentComment>());

            public Task RemoveAsync(IncidentComment comment, CancellationToken ct)
                => Task.CompletedTask;
        }

        private sealed class FakeUow : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
        }

        [Fact]
        public async Task Creates_Comment_When_Incident_Exists_And_User_Exists()
        {
            // Arrange
            var repo = new FakeCommentsRepo { IncidentExists = true };
            var uow = new FakeUow();

            var users = new Mock<IUserRepository>(MockBehavior.Strict);
            var incidentId = Guid.NewGuid();
            var authorId = Guid.NewGuid();

            users.Setup(x => x.ExistsByIdAsync(authorId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

            var handler = new CreateCommentCommandHandler(repo, uow, users.Object);

            // Act
            var dto = await handler.Handle(new CreateCommentCommand(incidentId, authorId, " hello "), CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, dto.Id);
            Assert.Equal(incidentId, dto.IncidentId);
            Assert.Equal(authorId, dto.UserId);
            Assert.Equal("hello", dto.Text); // trimmed
            Assert.NotEqual(default, dto.CreatedAtUtc);

            users.VerifyAll();
        }

        [Fact]
        public async Task Throws_404_When_Incident_Missing()
        {
            // Arrange
            var repo = new FakeCommentsRepo { IncidentExists = false };
            var uow = new FakeUow();

            var users = new Mock<IUserRepository>(MockBehavior.Loose);
            users.Setup(x => x.ExistsByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true); // user exists; want to isolate the incident-not-found path

            var handler = new CreateCommentCommandHandler(repo, uow, users.Object);

            // Act + Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "x"), CancellationToken.None));
        }

        [Fact]
        public async Task Throws_When_Author_Not_Found()
        {
            // Arrange
            var repo = new FakeCommentsRepo { IncidentExists = true };
            var uow = new FakeUow();

            var users = new Mock<IUserRepository>(MockBehavior.Strict);
            users.Setup(x => x.ExistsByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            var handler = new CreateCommentCommandHandler(repo, uow, users.Object);

            // Act + Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                handler.Handle(new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "x"), CancellationToken.None));

            users.VerifyAll();
        }
    }
}
