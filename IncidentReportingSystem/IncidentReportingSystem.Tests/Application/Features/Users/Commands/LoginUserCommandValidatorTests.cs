using IncidentReportingSystem.Application.Features.Users.Commands.LoginUser;

namespace IncidentReportingSystem.Tests.Application.Features.Users.Commands
{
    public sealed class LoginUserCommandValidatorTests
    {
        [Fact]
        public void Valid_Request_Passes()
        {
            var v = new LoginUserCommandValidator();
            var ok = v.Validate(new LoginUserCommand("alice@example.com", "P@ssw0rd!"));
            Assert.True(ok.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        public void Invalid_Email_Fails(string email)
        {
            var v = new LoginUserCommandValidator();
            var result = v.Validate(new LoginUserCommand(email, "P@ssw0rd!"));
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        public void Invalid_Password_Fails(string pwd)
        {
            var v = new LoginUserCommandValidator();
            var result = v.Validate(new LoginUserCommand("alice@example.com", pwd));
            Assert.False(result.IsValid);
        }
    }
}