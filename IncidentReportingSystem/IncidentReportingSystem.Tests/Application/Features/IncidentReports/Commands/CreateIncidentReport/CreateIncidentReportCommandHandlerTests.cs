using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;

using Xunit;

namespace IncidentReportingSystem.Tests.Application.Features.IncidentReports.Commands.CreateIncidentReport
{
    public class CreateIncidentReportCommandHandlerTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldCreateIncidentReportSuccessfully()
        {
            // Arrange
            var mockRepo = TestMockFactory.CreateIncidentReportRepository();
            var mockCurrentUser = new Mock<ICurrentUser>();
            var mockUserId = Guid.NewGuid();
            mockCurrentUser.SetupGet(user => user.UserId).Returns(mockUserId.ToString());
            var handler = new CreateIncidentReportCommandHandler(mockRepo.Object, mockCurrentUser.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(command.Description, result.Description);
            Assert.Equal(command.Location, result.Location);
            Assert.Equal(mockUserId, result.ReporterId);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldCallSaveAsyncOnce()
        {
            // Arrange
            var mockRepo = TestMockFactory.CreateIncidentReportRepository();
            var mockCurrentUser = new Mock<ICurrentUser>();
            var mockUserId = Guid.NewGuid();
            mockCurrentUser.SetupGet(user => user.UserId).Returns(mockUserId.ToString());
            var handler = new CreateIncidentReportCommandHandler(mockRepo.Object, mockCurrentUser.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            mockRepo.Verify(x => x.SaveAsync(It.IsAny<IncidentReport>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldRespectCancellationToken()
        {
            // Arrange
            var repository = TestMockFactory.CreateIncidentReportRepository();
            var mockCurrentUser = new Mock<ICurrentUser>();
            var mockUserId = Guid.NewGuid();
            mockCurrentUser.SetupGet(user => user.UserId).Returns(mockUserId.ToString());
            var handler = new CreateIncidentReportCommandHandler(repository.Object, mockCurrentUser.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                handler.Handle(command, cts.Token)
            );
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Handle_ShouldThrow_WhenRepositoryFails()
        {
            // Arrange
            var mockRepo = TestMockFactory.CreateIncidentReportRepository();
            mockRepo.Setup(r => r.SaveAsync(It.IsAny<IncidentReport>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("DB error"));

            var mockCurrentUser = new Mock<ICurrentUser>();
            var handler = new CreateIncidentReportCommandHandler(mockRepo.Object, mockCurrentUser.Object);
            var command = TestMockFactory.CreateValidCreateCommand();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(command, CancellationToken.None));
        }
    }
}
