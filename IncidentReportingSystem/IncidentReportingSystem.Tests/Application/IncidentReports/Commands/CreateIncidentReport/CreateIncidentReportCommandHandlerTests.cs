using System;
using System.Threading;
using System.Threading.Tasks;

using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Tests.Helpers;

using Moq;

using Xunit;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Commands
{
    public class CreateIncidentReportCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ShouldCreateIncidentReportSuccessfully()
        {
            // Arrange
            var mockRepo = TestMockFactory.CreateIncidentReportRepository();
            var handler = new CreateIncidentReportCommandHandler(mockRepo.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(command.Description, result.Description);
            Assert.Equal(command.Location, result.Location);
            Assert.Equal(command.ReporterId, result.ReporterId);
        }

        [Fact]
        public async Task Handle_ShouldCallSaveAsyncOnce()
        {
            // Arrange
            var mockRepo = TestMockFactory.CreateIncidentReportRepository();
            var handler = new CreateIncidentReportCommandHandler(mockRepo.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            mockRepo.Verify(x => x.SaveAsync(It.IsAny<IncidentReport>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldRespectCancellationToken()
        {
            // Arrange
            var repository = TestMockFactory.CreateIncidentReportRepository();
            var handler = new CreateIncidentReportCommandHandler(repository.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                handler.Handle(command, cts.Token)
            );
        }

        [Fact]
        public async Task Handle_ShouldThrow_WhenRepositoryFails()
        {
            // Arrange
            var mockRepo = TestMockFactory.CreateIncidentReportRepository();
            mockRepo.Setup(r => r.SaveAsync(It.IsAny<IncidentReport>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("DB error"));

            var handler = new CreateIncidentReportCommandHandler(mockRepo.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }
}
