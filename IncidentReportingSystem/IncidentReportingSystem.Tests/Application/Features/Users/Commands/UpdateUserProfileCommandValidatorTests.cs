using FluentAssertions;
using IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Tests.Application.Features.Users.Commands
{
    public sealed class UpdateUserProfileCommandValidatorTests
    {
        private readonly UpdateUserProfileCommandValidator _v = new();

        [Theory]
        [InlineData("Guy", "Sne")]
        [InlineData("Anne-Marie", "O'Neil")]
        [InlineData("Max", "Brenner")]
        [InlineData("A", "B")]
        [InlineData("First Second", "Third")]
        public void Valid_Names_Pass(string first, string last)
        {
            var res = _v.Validate(new UpdateUserProfileCommand(first, last));
            res.IsValid.Should().BeTrue(res.ToString());
        }

        [Theory]
        [InlineData("", "Sne")]
        [InlineData("Guy", "")]
        [InlineData(" ", "Last")]
        [InlineData("First", " ")]
        [InlineData("G@uy", "Sne")]
        [InlineData("Guy", "Sn#e")]
        [InlineData("ThisNameIsWayTooLongForTheAllowedFiftyCharacters_XXXX", "Sne")]
        public void Invalid_Names_Fail(string first, string last)
        {
            var res = _v.Validate(new UpdateUserProfileCommand(first, last));
            res.IsValid.Should().BeFalse();
        }
    }
}
