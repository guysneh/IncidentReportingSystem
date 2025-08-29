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

        [Fact]
        [Trait("Category", "Unit")]
        public void FirstName_And_LastName_Are_Optional_But_Trimmed_And_Max100()
        {
            var ok100 = new string('x', 100);
            var cmdOk = new RegisterUserCommand(
                "u@example.com", "P@ssw0rd!", new[] { Roles.User },
                FirstName: "  Ada  ", LastName: "  Lovelace  "
            );
            var r1 = _v.Validate(cmdOk);
            r1.IsValid.Should().BeTrue("names supplied within 100 chars are valid");

            var tooLong = new string('y', 101);
            var cmdTooLongFirst = new RegisterUserCommand("a1@example.com", "P@ssw0rd!", new[] { Roles.User }, FirstName: tooLong, LastName: "Ok");
            var cmdTooLongLast = new RegisterUserCommand("a2@example.com", "P@ssw0rd!", new[] { Roles.User }, FirstName: "Ok", LastName: tooLong);

            _v.Validate(cmdTooLongFirst).IsValid.Should().BeFalse("firstName > 100 must fail");
            _v.Validate(cmdTooLongLast).IsValid.Should().BeFalse("lastName > 100 must fail");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Names_Can_Be_Null_Or_Whitespace()
        {
            var cmdNull = new RegisterUserCommand("n1@example.com", "P@ssw0rd!", new[] { Roles.User }, FirstName: null, LastName: null);
            _v.Validate(cmdNull).IsValid.Should().BeTrue("null names are allowed");

            var cmdWs = new RegisterUserCommand("n2@example.com", "P@ssw0rd!", new[] { Roles.User }, FirstName: "   ", LastName: "\t");
            _v.Validate(cmdWs).IsValid.Should().BeTrue("whitespace names are treated as empty and allowed");
        }
    }
}
