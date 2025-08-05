using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Enums;
using FluentAssertions;

public class CreateIncidentReportCommandTests
{
    [Fact]
    public void Constructor_Should_Set_All_Properties_Correctly()
    {
        // Arrange
        var description = "System failure";
        var location = "Berlin HQ";
        var reporterId = Guid.NewGuid();
        var category = IncidentCategory.Infrastructure;
        var systemAffected = "CoreRouter01";
        var severity = IncidentSeverity.High;
        var reportedAt = new DateTime(2025, 8, 5, 13, 0, 0);

        // Act
        var command = new CreateIncidentReportCommand(
            description,
            location,
            reporterId,
            category,
            systemAffected,
            severity,
            reportedAt
        );

        // Assert
        command.Description.Should().Be(description);
        command.Location.Should().Be(location);
        command.ReporterId.Should().Be(reporterId);
        command.Category.Should().Be(category);
        command.SystemAffected.Should().Be(systemAffected);
        command.Severity.Should().Be(severity);
        command.ReportedAt.Should().Be(reportedAt);
    }
}
