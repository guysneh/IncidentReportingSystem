using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Controllers;

/// <summary>
/// Controller for retrieving aggregated incident statistics.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[ApiVersion("1.0")]
public class IncidentStatisticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IncidentStatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves high-level incident statistics (e.g., counts per severity).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IncidentStatisticsDto>> GetStatistics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIncidentStatisticsQuery(), cancellationToken);
        return Ok(result);
    }
}
