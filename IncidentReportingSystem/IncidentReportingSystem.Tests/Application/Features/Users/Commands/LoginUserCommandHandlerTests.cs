using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Exceptions;
using IncidentReportingSystem.Application.Features.Users.Commands.LoginUser;
using Moq;

namespace IncidentReportingSystem.Tests.Application.Features.Users.Commands
{
    public sealed class LoginUserCommandHandlerTests
    {
        private static LoginUserCommandHandler Make(out Mock<IUserRepository> users, out Mock<IPasswordHasher> hasher, out Mock<IJwtTokenService> jwt, IncidentReportingSystem.Domain.Entities.User? seeded = null)
        {
            users = new Mock<IUserRepository>();
            hasher = new Mock<IPasswordHasher>();
            jwt = new Mock<IJwtTokenService>();

            if (seeded is not null)
            {
                users.Setup(x => x.FindByNormalizedEmailAsync(seeded.NormalizedEmail, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(seeded);
            }

            jwt.Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, string>?>()))
               .Returns(("token", DateTimeOffset.UtcNow.AddHours(1)));

            return new LoginUserCommandHandler(users.Object, hasher.Object, jwt.Object);
        }

        [Fact]
        public async Task Success_Returns_Token()
        {
            var user = new IncidentReportingSystem.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "alice@example.com",
                NormalizedEmail = "ALICE@EXAMPLE.COM",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 2 },
            };
            user.SetRoles(new[] { "User" });

            var handler = Make(out var users, out var hasher, out var jwt, user);
            hasher.Setup(x => x.Verify("P@ssw0rd!", user.PasswordHash, user.PasswordSalt)).Returns(true);

            var result = await handler.Handle(new LoginUserCommand(user.Email, "P@ssw0rd!"), CancellationToken.None);
            Assert.Equal("token", result.AccessToken);
        }

        [Fact]
        public async Task Wrong_Password_Throws()
        {
            var user = new IncidentReportingSystem.Domain.Entities.User { Email = "alice@example.com", NormalizedEmail = "ALICE@EXAMPLE.COM", PasswordHash = new byte[] { 1 }, PasswordSalt = new byte[] { 2 }};
            user.SetRoles(new[] { "User" });
            var handler = Make(out var users, out var hasher, out var jwt, user);
            hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(false);
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new LoginUserCommand(user.Email, "bad"), CancellationToken.None));
        }

        [Fact]
        public async Task User_Not_Found_Throws()
        {
            var handler = Make(out var users, out var hasher, out var jwt);
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(new LoginUserCommand("nouser@example.com", "whatever"), CancellationToken.None));
        }
    }
}