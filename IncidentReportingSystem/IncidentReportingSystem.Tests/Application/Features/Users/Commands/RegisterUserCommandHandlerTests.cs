using FluentAssertions;
using IncidentReportingSystem.Domain.Entities;
using Moq;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using IncidentReportingSystem.Application.Exceptions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Domain;

namespace IncidentReportingSystem.Tests.Application.Features.Users.Commands
{
    public class RegisterUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IPasswordHasher> _hasher = new();
        private readonly RegisterUserCommandHandler _handler;

        public RegisterUserCommandHandlerTests()
        {
            _handler = new RegisterUserCommandHandler(_userRepo.Object, _uow.Object, _hasher.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateUser_WhenValidRequest()
        {
            // Arrange
            var cmd = new RegisterUserCommand("test@example.com", "P@ssw0rd!", new[] { Roles.User });

            _userRepo
                .Setup(r => r.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _hasher
                .Setup(h => h.HashPassword(cmd.Password))
                .Returns((new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 }));

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.Email.Should().Be("test@example.com");
            result.Roles.Should().Contain(Roles.User);

            _userRepo.Verify(r =>
                r.AddAsync(It.Is<User>(u => u.Email == "test@example.com"),
                           It.IsAny<CancellationToken>()), Times.Once);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _hasher.Verify(h => h.HashPassword(cmd.Password), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenEmailExists()
        {
            var cmd = new RegisterUserCommand("test@example.com", "pass", new[] { Roles.User });

            _userRepo
                .Setup(r => r.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await FluentActions.Awaiting(() => _handler.Handle(cmd, CancellationToken.None))
                .Should().ThrowAsync<EmailAlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenRoleInvalid()
        {
            var cmd = new RegisterUserCommand("test@example.com", "pass", new[] { "InvalidRole" });

            _userRepo
                .Setup(r => r.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await FluentActions.Awaiting(() => _handler.Handle(cmd, CancellationToken.None))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("*invalid*");
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenRequestNull()
        {
            await FluentActions.Awaiting(() => _handler.Handle(null!, CancellationToken.None))
                .Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
