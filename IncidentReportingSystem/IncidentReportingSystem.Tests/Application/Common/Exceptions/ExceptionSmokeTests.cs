using System;
using FluentAssertions;
using Xunit;
using IncidentReportingSystem.Application.Common.Exceptions;

namespace IncidentReportingSystem.Tests.Application.Common.Exceptions
{
    public sealed class ExceptionSmokeTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void NotFoundException_Message_Composed()
        {
            var ex = new NotFoundException("Incident", "42");
            ex.Message.Should().Contain("Incident '42' was not found.");
            ex.Resource.Should().Be("Incident");
            ex.Key.Should().Be("42");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void EmailAlreadyExists_SingleArg()
        {
            var ex = new EmailAlreadyExistsException("a@b.com");
            ex.Email.Should().Be("a@b.com");
            ex.Message.Should().Contain("a@b.com");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void InvalidCredentials_And_Forbidden()
        {
            new InvalidCredentialsException().Message.Should().NotBeNullOrEmpty();
            new ForbiddenException("nope").Message.Should().Contain("nope");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void AccountLocked_With_And_Without_EndTime()
        {
            var withEnd = new AccountLockedException(DateTimeOffset.UtcNow.AddMinutes(5));
            withEnd.Message.Should().Contain("Account is locked until");

            var withoutEnd = new AccountLockedException(null);
            withoutEnd.Message.Should().Be("Account is locked.");
        }
    }
}
