using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile;
using IncidentReportingSystem.Domain.Entities;
using Moq;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.Users.Commands
{
    public sealed class UpdateUserProfileCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Updates_Names_And_Touches_ModifiedAt()
        {
            // arrange
            var id = Guid.NewGuid();
            var user = new User
            {
                Id = id,
                Email = "user@example.com",
                FirstName = "Old",
                LastName = "Name",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            var repo = new Mock<IUserRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1);
            var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
            current.Setup(c => c.UserIdOrThrow()).Returns(id);

            var handler = new UpdateUserProfileCommandHandler(repo.Object, uow.Object, current.Object);

            // act
            var dto = await handler.Handle(
                new UpdateUserProfileCommand("  Guy   ", "  Sne   "),
                CancellationToken.None);

            // assert
            dto.Id.Should().Be(id);
            dto.FirstName.Should().Be("Guy");
            dto.LastName.Should().Be("Sne");
            dto.DisplayName.Should().Be("Guy Sne");
            dto.ModifiedAtUtc.Should().NotBeNull();

            uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_User_NotFound_Throws()
        {
            var id = Guid.NewGuid();

            var repo = new Mock<IUserRepository>(MockBehavior.Strict);
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
            current.Setup(c => c.UserIdOrThrow()).Returns(id);

            var handler = new UpdateUserProfileCommandHandler(repo.Object, uow.Object, current.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new UpdateUserProfileCommand("A", "B"), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_Collapses_Extra_Spaces()
        {
            var id = Guid.NewGuid();
            var user = new User { Id = id, Email = "user@example.com", CreatedAtUtc = DateTime.UtcNow };

            var repo = new Mock<IUserRepository>();
            repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var current = new Mock<ICurrentUserService>();
            current.Setup(c => c.UserIdOrThrow()).Returns(id);

            var handler = new UpdateUserProfileCommandHandler(repo.Object, uow.Object, current.Object);

            var dto = await handler.Handle(
                new UpdateUserProfileCommand("  John   Paul  ", "  Van   Damme "),
                CancellationToken.None);

            dto.FirstName.Should().Be("John Paul");
            dto.LastName.Should().Be("Van Damme");
            dto.DisplayName.Should().Be("John Paul Van Damme");
        }
    }
}
