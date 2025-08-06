using FluentValidation.TestHelper;

using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Application.IncidentReports.Validators;
using IncidentReportingSystem.Domain.Enums;

using Xunit;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Commands.CreateIncidentReport;

public class CreateIncidentReportCommandValidatorTests
{
    private readonly CreateIncidentReportCommandValidator _validator = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Have_Error_When_Description_Is_Empty()
    {
        var command = new CreateIncidentReportCommand(
            "", // Invalid Description
            "Berlin",
            Guid.NewGuid(),
            IncidentCategory.Security,
            "Firewall",
            IncidentSeverity.High,
            DateTime.UtcNow
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Have_Error_When_Location_Is_Too_Long()
    {
        var longLocation = new string('A', 256);

        var command = new CreateIncidentReportCommand(
            "Outage",
            longLocation,
            Guid.NewGuid(),
            IncidentCategory.Security,
            "Postgres",
            IncidentSeverity.Medium,
            DateTime.UtcNow
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Have_Error_When_ReporterId_Is_Empty()
    {
        var command = new CreateIncidentReportCommand(
            "System Error",
            "Berlin",
            Guid.Empty, // Invalid
            IncidentCategory.Security,
            "Router",
            IncidentSeverity.Low,
            DateTime.UtcNow
        );

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReporterId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_Pass_When_All_Fields_Are_Valid()
    {
        var command = new CreateIncidentReportCommand(
            "System outage in datacenter",
            "Frankfurt",
            Guid.NewGuid(),
            IncidentCategory.Infrastructure,
            "PowerGrid",
            IncidentSeverity.High,
            DateTime.UtcNow
        );

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
