using FluentAssertions;
using IncidentReportingSystem.API.Controllers;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReportById;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.Tests.API.Controllers
{
    [Trait("Category", "Unit")]
    public sealed class IncidentReportsControllerFlowTests
    {
        private static IncidentReportsController Ctrl(Mock<IMediator> m = null!)
            => new IncidentReportsController((m ?? new Mock<IMediator>()).Object);

        [Fact]
        public async Task GetAll_Delegates_To_Mediator_And_Returns_Ok()
        {
            var sent = (GetIncidentReportsQuery?)null;
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<GetIncidentReportsQuery>(), It.IsAny<CancellationToken>()))
             .Callback<object, CancellationToken>((q, _) => sent = (GetIncidentReportsQuery)q)
             .ReturnsAsync(Array.Empty<IncidentReport>());

            var ctrl = Ctrl(m);
            var res = await ctrl.GetAll(status: IncidentStatus.Open, skip: 1, take: 2,
                                        category: IncidentCategory.Security, severity: IncidentSeverity.Medium,
                                        searchText: "txt", reportedAfter: DateTime.UtcNow.AddDays(-1),
                                        reportedBefore: DateTime.UtcNow, sortBy: IncidentSortField.CreatedAt,
                                        direction: SortDirection.Asc, cancellationToken: CancellationToken.None);

            res.Should().BeOfType<OkObjectResult>();
            sent.Should().NotBeNull();
            sent!.Skip.Should().Be(1);
            sent.Take.Should().Be(2);
            sent.Status.Should().Be(IncidentStatus.Open);
        }

        [Fact]
        public async Task BulkStatus_Delegates_Command_And_Returns_Ok()
        {
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var sent = (BulkUpdateIncidentStatusCommand?)null;
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<BulkUpdateIncidentStatusCommand>(), It.IsAny<CancellationToken>()))
             .Callback<object, CancellationToken>((c, _) => sent = (BulkUpdateIncidentStatusCommand)c)
             .ReturnsAsync(new BulkStatusUpdateResultDto { Updated = 2, NotFound = Array.Empty<Guid>(), IdempotencyKey = "k1" });

            var ctrl = Ctrl(m);
            var res = await ctrl.BulkStatus(
                new IncidentReportsController.BulkStatusUpdateRequest(ids, IncidentStatus.Closed),
                idempotencyKey: "k1", ct: CancellationToken.None);

            res.Should().BeOfType<OkObjectResult>();
            sent.Should().NotBeNull();
            sent!.IdempotencyKey.Should().Be("k1");
            sent.Ids.Should().BeEquivalentTo(ids);
            sent.NewStatus.Should().Be(IncidentStatus.Closed);
        }

        [Fact]
        public async Task GetById_Bubbles_KeyNotFoundException()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<GetIncidentReportByIdQuery>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("not found"));

            var ctrl = Ctrl(m);
            var act = async () => await ctrl.GetById(Guid.NewGuid(), CancellationToken.None);
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task UpdateStatus_Bubbles_KeyNotFoundException()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<UpdateIncidentStatusCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("missing"));

            var ctrl = Ctrl(m);
            var act = async () => await ctrl.UpdateStatus(Guid.NewGuid(), IncidentStatus.Closed, CancellationToken.None);
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
