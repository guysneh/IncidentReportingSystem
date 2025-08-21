using FluentValidation.TestHelper;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Commands.CreateIncidentReport;

public class UpdateIncidentStatusValidatorTests
{
    private readonly UpdateIncidentStatusValidator _validator;

    public UpdateIncidentStatusValidatorTests()
    {
        _validator = new UpdateIncidentStatusValidator();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        // Arrange
        var command = new UpdateIncidentStatusCommand(Guid.Empty, IncidentStatus.Closed);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Have_Error_When_Status_Is_Not_Enum()
    {
        // Arrange
        var invalidStatus = (IncidentStatus)999; // deliberately out of range
        var command = new UpdateIncidentStatusCommand(Guid.NewGuid(), invalidStatus);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewStatus);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Not_Have_Any_Errors_For_Valid_Command()
    {
        // Arrange
        var command = new UpdateIncidentStatusCommand(Guid.NewGuid(), IncidentStatus.Open);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
