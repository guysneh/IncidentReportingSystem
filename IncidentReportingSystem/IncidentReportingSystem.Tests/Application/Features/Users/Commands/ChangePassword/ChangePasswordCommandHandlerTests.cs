// Tests/Application/Users/Commands/ChangePassword/ChangePasswordCommandHandlerTests.cs
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Features.Users.Commands.ChangePassword;
using IncidentReportingSystem.Domain.Entities;
using Moq;
using NSubstitute;
using Xunit;

public sealed class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task Fails_When_CurrentPassword_Wrong()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@e.com", PasswordHash = new byte[] { 1 }, PasswordSalt = new byte[] { 2 }, CreatedAtUtc = DateTime.UtcNow };
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var uow = new Mock<IUnitOfWork>();
        var current = new Mock<ICurrentUserService>();
        current.Setup(c => c.UserIdOrThrow()).Returns(user.Id);

        var pwd = new Mock<IPasswordHasher>();
        pwd.Setup(p => p.Verify("wrong", user.PasswordHash, user.PasswordSalt)).Returns(false);

        var handler = new ChangePasswordCommandHandler(users.Object, uow.Object, current.Object, pwd.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ChangePasswordCommandHandler>>());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ChangePasswordCommand("wrong", "NewStrongP@ssw0rd"), CancellationToken.None));
    }

    [Fact]
    public async Task Updates_Hash_And_Saves()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@e.com", PasswordHash = new byte[] { 1 }, PasswordSalt = new byte[] { 2 }, CreatedAtUtc = DateTime.UtcNow };
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var current = new Mock<ICurrentUserService>();
        current.Setup(c => c.UserIdOrThrow()).Returns(user.Id);

        var pwd = new Mock<IPasswordHasher>();
        pwd.Setup(p => p.Verify("OldGood1!", user.PasswordHash, user.PasswordSalt)).Returns(true);
        pwd.Setup(p => p.HashPassword("VeryStrongP@ssw0rd!")).Returns((new byte[] { 9, 9 }, new byte[] { 8, 8 }));

        var handler = new ChangePasswordCommandHandler(users.Object, uow.Object, current.Object, pwd.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<ChangePasswordCommandHandler>>());

        await handler.Handle(new ChangePasswordCommand("OldGood1!", "VeryStrongP@ssw0rd!"), CancellationToken.None);

        Assert.Equal(new byte[] { 9, 9 }, user.PasswordHash);
        Assert.Equal(new byte[] { 8, 8 }, user.PasswordSalt);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
