using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using IncidentReportingSystem.API.Controllers;
using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentStatistics;

namespace IncidentReportingSystem.Tests.API.Controllers
{
    [Trait("Category", "Unit")]
    public sealed class IncidentStatisticsControllerTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task GetStatistics_Returns_Ok_With_Dto()
        {
            var dto = new IncidentStatisticsDto { TotalIncidents = 42 };
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<GetIncidentStatisticsQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(dto);

            var ctrl = new IncidentStatisticsController(m.Object);
            var res = await ctrl.GetStatistics(CancellationToken.None);

            res.Result.Should().BeOfType<OkObjectResult>();
            (res.Result as OkObjectResult)!.Value.Should().Be(dto);
            m.VerifyAll();
        }
    }
}
