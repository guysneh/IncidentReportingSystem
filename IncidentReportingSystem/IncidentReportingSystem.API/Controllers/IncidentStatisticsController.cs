using IncidentReportingSystem.Application.Features.Statistics.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.Api.Controllers.V1;

[ApiController]
[Route("api/v1/stats/incidents")]
public sealed class IncidentStatsController : ControllerBase
{
    private readonly ISender _sender;
    public IncidentStatsController(ISender sender) => _sender = sender;

    // GET /api/v1/stats/incidents/series?from=2025-06-01&to=2025-09-10&granularity=week
    [HttpGet("series")]
    [ProducesResponseType(typeof(IReadOnlyList<IncidentSeriesPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSeries(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] TimeGranularity granularity = TimeGranularity.Day,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetIncidentSeriesQuery(from, to, granularity), ct);
        return Ok(result);
    }
}
