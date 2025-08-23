using FluentAssertions;
using Xunit;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using IncidentReportingSystem.Application.Users.Commands.RegisterUser;
using IncidentReportingSystem.Domain;

namespace IncidentReportingSystem.Tests.Application.Features.Users.Commands
{
    public sealed class RegisterUserCommandValidatorAdditionalTests
    {
        private readonly RegisterUserCommandValidator _v = new();

        [Fact]
        [Trait("Category", "Unit")]
        public void Roles_Null_Fails()
        {
            var cmd = new RegisterUserCommand("a@b.com", "P@ssw0rd!", null!);
            var r = _v.Validate(cmd);
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Roles_Empty_Fails()
        {
            var cmd = new RegisterUserCommand("a@b.com", "P@ssw0rd!", new string[0]);
            var r = _v.Validate(cmd);
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Roles_Invalid_Fails()
        {
            var cmd = new RegisterUserCommand("a@b.com", "P@ssw0rd!", new[] { "NotARole" });
            var r = _v.Validate(cmd);
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Email_Too_Long_Fails()
        {
            var longEmail = new string('a', 321) + "@x.com"; 
            var cmd = new RegisterUserCommand(longEmail, "P@ssw0rd!", new[] { Roles.User });
            var r = _v.Validate(cmd);
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Valid_Request_Passes()
        {
            var cmd = new RegisterUserCommand("user@example.com", "P@ssw0rd!", new[] { Roles.User, Roles.Admin });
            var r = _v.Validate(cmd);
            r.IsValid.Should().BeTrue();
        }
    }
}
