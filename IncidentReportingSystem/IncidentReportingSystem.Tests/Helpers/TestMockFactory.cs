using System;

using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;

using Moq;

namespace IncidentReportingSystem.Tests.Helpers
{
    public static class TestMockFactory
    {
        public static Mock<IIncidentReportRepository> CreateIncidentReportRepository()
        {
            var mock = new Mock<IIncidentReportRepository>();

            mock.Setup(r => r.SaveAsync(It.IsAny<IncidentReportingSystem.Domain.Entities.IncidentReport>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mock;
        }

        public static CreateIncidentReportCommand CreateValidCreateCommand()
        {
            return new CreateIncidentReportCommand(
                Description: "Test incident",
                Location: "Berlin",
                ReporterId: Guid.NewGuid(),
                Category: IncidentCategory.Infrastructure,
                SystemAffected: "PowerGrid",
                Severity: IncidentSeverity.Medium,
                ReportedAt: DateTime.UtcNow
            );
        }
    }
}
