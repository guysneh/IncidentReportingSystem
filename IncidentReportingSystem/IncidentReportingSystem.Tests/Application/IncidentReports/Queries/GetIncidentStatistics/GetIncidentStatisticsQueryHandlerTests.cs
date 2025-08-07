using FluentAssertions;
using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentStatistics;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Tests.Helpers;
using Moq;
using Shouldly;


namespace IncidentReportingSystem.Tests.Application.IncidentReports.Queries.GetIncidentStatistics
{
    public class GetIncidentStatisticsQueryHandlerTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var incidents = new List<IncidentReport>
            {
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), category: IncidentCategory.Infrastructure, severity: IncidentSeverity.High),
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), category: IncidentCategory.Infrastructure, severity: IncidentSeverity.Medium),
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), category: IncidentCategory.ITSystems, severity: IncidentSeverity.Low),
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), category: IncidentCategory.Security, severity: IncidentSeverity.High),
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), category: IncidentCategory.Security, severity: IncidentSeverity.High)
            };

            var mockRepo = new Mock<IIncidentReportRepository>();
            mockRepo.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(incidents);

            var handler = new GetIncidentStatisticsQueryHandler(mockRepo.Object);
            var query = new GetIncidentStatisticsQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.ShouldNotBeNull();
            result.TotalIncidents.ShouldBe(5);
            result.IncidentsByCategory["Infrastructure"].ShouldBe(2);
            result.IncidentsByCategory["ITSystems"].ShouldBe(1);
            result.IncidentsByCategory["Security"].ShouldBe(2);

            result.IncidentsBySeverity["High"].ShouldBe(3);
            result.IncidentsBySeverity["Medium"].ShouldBe(1);
            result.IncidentsBySeverity["Low"].ShouldBe(1);
        }
    }
}
