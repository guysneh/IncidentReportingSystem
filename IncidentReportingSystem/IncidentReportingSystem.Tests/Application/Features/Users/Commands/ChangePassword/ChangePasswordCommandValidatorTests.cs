// Tests/Application/Users/Commands/ChangePassword/ChangePasswordCommandValidatorTests.cs
using FluentValidation.TestHelper;
using IncidentReportingSystem.Application.Features.Users.Commands.ChangePassword;
using Xunit;

public sealed class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _v = new();

    [Theory]
    [InlineData("short", "short")]
    [InlineData("CurrentGood1!", "CurrentGood1!")] // same new==current
    public void Invalid_Passwords_Fail(string current, string @new)
    {
        var res = _v.TestValidate(new ChangePasswordCommand(current, @new));
        res.ShouldHaveValidationErrors();
    }

    [Fact]
    public void Strong_Password_Passes()
    {
        var res = _v.TestValidate(new ChangePasswordCommand("OldGood1!", "VeryStrongP@ssw0rd!"));
        res.ShouldNotHaveAnyValidationErrors();
    }
}
