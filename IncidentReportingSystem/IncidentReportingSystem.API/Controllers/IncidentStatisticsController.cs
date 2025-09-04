using Asp.Versioning;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentStatistics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Controllers;

/// <summary>
/// Controller for retrieving aggregated incident statistics.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Tags("Statistics")]
public class IncidentStatisticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IncidentStatisticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Retrieves aggregated incident statistics (counts per severity, etc.).</summary>
    /// <response code="200">Statistics returned successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Not authorized to read incidents.</response>
    [HttpGet]
    [Authorize(Policy = PolicyNames.CanReadIncidents)]
    [ProducesResponseType(typeof(IncidentStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IncidentStatisticsDto>> GetStatistics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIncidentStatisticsQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
