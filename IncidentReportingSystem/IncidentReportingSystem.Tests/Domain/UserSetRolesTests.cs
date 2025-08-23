using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using IncidentReportingSystem.Domain;
using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Tests.Domain
{
    public sealed class UserSetRolesTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void SetRoles_Throws_On_Null()
        {
            var u = new User { Email = "a@b.com", NormalizedEmail = "A@B.COM" };
            Action act = () => u.SetRoles(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("roles");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void SetRoles_Throws_On_Empty_After_Filtering()
        {
            var u = new User { Email = "a@b.com", NormalizedEmail = "A@B.COM" };
            Action act = () => u.SetRoles(new[] { " ", "\t", "" });
            act.Should().Throw<ArgumentException>()
               .WithMessage("*At least one role is required*")
               .And.ParamName.Should().Be("roles");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void SetRoles_Throws_When_Invalid_Role_Present()
        {
            var u = new User { Email = "a@b.com", NormalizedEmail = "A@B.COM" };
            Action act = () => u.SetRoles(new[] { Roles.User, "NotARole" });
            act.Should().Throw<ArgumentException>()
               .WithMessage("*One or more roles are invalid*")
               .And.ParamName.Should().Be("roles");
        }
    }
}
