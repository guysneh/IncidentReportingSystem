using FluentAssertions;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReportById;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Tests.Helpers;
using Moq;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Queries.GetIncidentReportById
{
    public class GetIncidentReportByIdQueryHandlerTests
    {
        private readonly Mock<IIncidentReportRepository> _repositoryMock;
        private readonly GetIncidentReportByIdQueryHandler _handler;

        public GetIncidentReportByIdQueryHandlerTests()
        {
            _repositoryMock = TestMockFactory.CreateIncidentReportRepositoryMock();
            _handler = new GetIncidentReportByIdQueryHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnIncidentReport_WhenReportExists()
        {
            // Arrange
            var incidentId = Guid.NewGuid();
            var expectedReport = TestMockFactory.CreateIncidentReport(incidentId, "Broken pipe", "Basement");

            _repositoryMock
                .Setup(r => r.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedReport);

            var query = new GetIncidentReportByIdQuery(incidentId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedReport);
            _repositoryMock.Verify(r => r.GetByIdAsync(incidentId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowKeyNotFoundException_WhenReportDoesNotExist()
        {
            // Arrange
            var missingId = Guid.NewGuid();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((IncidentReport?)null);

            var query = new GetIncidentReportByIdQuery(missingId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Incident with ID '{missingId}' was not found.");

            _repositoryMock.Verify(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
