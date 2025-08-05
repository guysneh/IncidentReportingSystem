using FluentAssertions;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Tests.Helpers;
using Moq;

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
            var query = new GetIncidentReportsQuery(
                IncludeClosed: true,
                Skip: 0,
                Take: 10
            );

            var expectedReports = new List<IncidentReport>
            {
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), "desc1", "Berlin"),
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), "desc2", "Hamburg")
            };

            _repositoryMock
                .Setup(r => r.GetAsync(
                    true, 0, 10,
                    null, null, null, null, null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReports);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedReports);
            _repositoryMock.Verify(r => r.GetAsync(
                true, 0, 10,
                null, null, null, null, null,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldPassFiltersToRepository()
        {
            // Arrange
            var query = new GetIncidentReportsQuery(
                IncludeClosed: false,
                Skip: 5,
                Take: 5,
                Category: IncidentCategory.Security,
                Severity: IncidentSeverity.High,
                SearchText: "Berlin",
                ReportedAfter: new DateTime(2023, 1, 1),
                ReportedBefore: new DateTime(2023, 12, 31)
            );

            var expectedReports = new List<IncidentReport>
            {
                TestMockFactory.CreateIncidentReport(Guid.NewGuid(), "Security issue", "Berlin")
            };

            _repositoryMock
                .Setup(r => r.GetAsync(
                    false, 5, 5,
                    IncidentCategory.Security,
                    IncidentSeverity.High,
                    "Berlin",
                    new DateTime(2023, 1, 1),
                    new DateTime(2023, 12, 31),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReports);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expectedReports);
            _repositoryMock.Verify(r => r.GetAsync(
                false, 5, 5,
                IncidentCategory.Security,
                IncidentSeverity.High,
                "Berlin",
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 31),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoReportsFound()
        {
            // Arrange
            var query = new GetIncidentReportsQuery();

            _repositoryMock
                .Setup(r => r.GetAsync(
                    false, 0, 50,
                    null, null, null, null, null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IncidentReport>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
            _repositoryMock.Verify(r => r.GetAsync(
                false, 0, 50,
                null, null, null, null, null,
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
