using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Tests;
using IncidentReportingSystem.Tests.Helpers;
using Moq;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Queries.GetIncidentReports
{
    public class GetIncidentReportsQueryHandlerTests
    {
        private readonly Mock<IIncidentReportRepository> _repositoryMock;
        private readonly GetIncidentReportsQueryHandler _handler;

        public GetIncidentReportsQueryHandlerTests()
        {
            _repositoryMock = TestMockFactory.CreateIncidentReportRepositoryMock();
            _handler = new GetIncidentReportsQueryHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnReports_FromRepository()
        {
            // Arrange
            var query = new GetIncidentReportsQuery(true, 0, 10);
            var expectedReports = new List<IncidentReport>
            {
                new IncidentReport("desc1", "Berlin", Guid.NewGuid(), IncidentCategory.Infrastructure, "Sys1", IncidentSeverity.High, DateTime.UtcNow),
                new IncidentReport("desc2", "Hamburg", Guid.NewGuid(), IncidentCategory.Security, "Sys2", IncidentSeverity.Low, DateTime.UtcNow)
            };

            _repositoryMock
                .Setup(r => r.GetAsync(true, 0, 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReports);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedReports);
            _repositoryMock.Verify(r => r.GetAsync(true, 0, 10, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
