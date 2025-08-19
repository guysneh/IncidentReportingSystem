using FluentAssertions;
using FluentValidation;
using IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;
using Moq;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Commands
{
    public class UpdateIncidentStatusCommandHandlerTests
    {
        private readonly Mock<IIncidentReportRepository> _repositoryMock;
        private readonly UpdateIncidentStatusCommandHandler _handler;

        public UpdateIncidentStatusCommandHandlerTests()
        {
            _repositoryMock = new Mock<IIncidentReportRepository>();
            _handler = new UpdateIncidentStatusCommandHandler(_repositoryMock.Object);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_Should_Update_Incident_Status_And_Save()
        {
            // Arrange
            var incidentId = Guid.NewGuid();
            var incident = new IncidentReport("desc", "loc", Guid.NewGuid(), IncidentCategory.ITSystems, "sys", IncidentSeverity.Medium, DateTime.UtcNow);
            _repositoryMock.Setup(r => r.GetByIdAsync(incidentId, It.IsAny<CancellationToken>())).ReturnsAsync(incident);

            var command = new UpdateIncidentStatusCommand(incidentId, IncidentStatus.Closed);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            incident.Status.Should().Be(IncidentStatus.Closed);
            _repositoryMock.Verify(r => r.SaveAsync(incident, It.IsAny<CancellationToken>()), Times.Once);
            result.Should().Be(MediatR.Unit.Value);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_Should_Throw_When_Incident_Not_Found()
        {
            // Arrange
            var incidentId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(incidentId, It.IsAny<CancellationToken>())).ReturnsAsync((IncidentReport?)null);

            var command = new UpdateIncidentStatusCommand(incidentId, IncidentStatus.InProgress);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"*{incidentId}*");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_Should_Respect_Cancellation()
        {
            // Arrange
            var incidentId = Guid.NewGuid();
            var command = new UpdateIncidentStatusCommand(incidentId, IncidentStatus.Open);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            Func<Task> act = async () => await _handler.Handle(command, cts.Token);

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
    }
}
