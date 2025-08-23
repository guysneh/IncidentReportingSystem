using FluentAssertions;
using IncidentReportingSystem.API.Controllers;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReportById;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.Tests.API.Controllers
{
    [Trait("Category", "Unit")]
    public sealed class IncidentReportsControllerValidationTests
    {
        private static IncidentReportsController NewController(IMediator? mediator = null)
            => new IncidentReportsController(mediator ?? Substitute.For<IMediator>());
        private static IncidentReportsController NewCtrl(Mock<IMediator> m = null!)
            => new IncidentReportsController((m ?? new Mock<IMediator>()).Object);


        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(0)]
        [InlineData(201)]
        public async Task GetAll_BadRequest_When_Take_OutOfRange(int take)
        {
            var ctrl = NewController();

            var res = await ctrl.GetAll(
                status: null, skip: 0, take: take, category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Desc,
                cancellationToken: CancellationToken.None);

            res.Should().BeOfType<BadRequestObjectResult>()
               .Which.Value!.ToString().Should().Contain("take");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAll_BadRequest_When_Skip_Negative()
        {
            var ctrl = NewController();

            var res = await ctrl.GetAll(
                status: null, skip: -1, take: 50, category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Desc,
                cancellationToken: CancellationToken.None);

            res.Should().BeOfType<BadRequestObjectResult>()
               .Which.Value!.ToString().Should().Contain("skip");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAll_BadRequest_When_ReportedAfter_Greater_Than_ReportedBefore()
        {
            var ctrl = NewController();

            var after = DateTime.UtcNow;
            var before = after.AddHours(-1);

            var res = await ctrl.GetAll(
                status: null, skip: 0, take: 50, category: null, severity: null,
                searchText: null, reportedAfter: after, reportedBefore: before,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Desc,
                cancellationToken: CancellationToken.None);

            res.Should().BeOfType<BadRequestObjectResult>()
               .Which.Value!.ToString().Should().Contain("reportedAfter");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetAll_Ok_When_Parameters_Valid()
        {
            var mediator = Substitute.For<IMediator>();
            mediator.Send(Arg.Any<GetIncidentReportsQuery>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<IReadOnlyList<IncidentReportingSystem.Domain.Entities.IncidentReport>>(
                        Array.Empty<IncidentReportingSystem.Domain.Entities.IncidentReport>()));

            var ctrl = NewController(mediator);

            var res = await ctrl.GetAll(
                status: null, skip: 0, take: 50, category: null, severity: null,
                searchText: null, reportedAfter: null, reportedBefore: null,
                sortBy: IncidentSortField.CreatedAt, direction: SortDirection.Desc,
                cancellationToken: CancellationToken.None);

            res.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task BulkStatus_BadRequest_When_Missing_IdempotencyKey()
        {
            var ctrl = NewController();
            var body = new IncidentReportsController.BulkStatusUpdateRequest(
                new List<Guid> { Guid.NewGuid() }, IncidentStatus.Open);

            var res = await ctrl.BulkStatus(body, idempotencyKey: "", ct: CancellationToken.None);

            res.Should().BeOfType<BadRequestObjectResult>()
               .Which.Value!.ToString().Should().Contain("Idempotency-Key");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task BulkStatus_BadRequest_When_Ids_Empty()
        {
            var ctrl = NewController();
            var body = new IncidentReportsController.BulkStatusUpdateRequest(
                new List<Guid>(), IncidentStatus.Open);

            var res = await ctrl.BulkStatus(body, idempotencyKey: "k1", ct: CancellationToken.None);

            res.Should().BeOfType<BadRequestObjectResult>()
               .Which.Value!.ToString().Should().Contain("Ids");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetById_Returns404_When_NotFound()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<GetIncidentReportByIdQuery>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("not found"));

            var ctrl = NewCtrl(m);
            var res = await ctrl.GetById(Guid.NewGuid(), CancellationToken.None);

            res.Should().BeOfType<NotFoundObjectResult>();
            m.VerifyAll();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task UpdateStatus_Returns404_When_NotFound()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<UpdateIncidentStatusCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new KeyNotFoundException("missing"));

            var ctrl = NewCtrl(m);
            var res = await ctrl.UpdateStatus(Guid.NewGuid(), /* newStatus: */ IncidentReportingSystem.Domain.Enums.IncidentStatus.Closed, CancellationToken.None);

            res.Should().BeOfType<NotFoundObjectResult>();
            m.VerifyAll();
        }
    }
}
