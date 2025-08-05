using FluentAssertions;
using IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Commands.UpdateIncidentStatus
{
    public class UpdateIncidentStatusCommandTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties_Correctly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var status = IncidentStatus.Closed;

            // Act
            var command = new UpdateIncidentStatusCommand(id, status);

            // Assert
            command.Id.Should().Be(id);
            command.NewStatus.Should().Be(status);
        }
    }
}
