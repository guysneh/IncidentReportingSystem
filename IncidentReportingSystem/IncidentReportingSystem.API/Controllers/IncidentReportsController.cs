using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReportById;
using IncidentReportingSystem.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// Handles API requests related to incident reports.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class IncidentReportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Constructor with injected MediatR handler.
        /// </summary>
        /// <param name="mediator">Mediator for sending commands.</param>
        public IncidentReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Creates a new incident report.
        /// </summary>
        /// <param name="command">Details of the incident to report.</param>
        /// <returns>The created incident report including its ID and metadata.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(IncidentReport), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateIncidentReportCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Retrieves an incident report by its unique ID.
        /// </summary>
        /// <param name="id">The incident ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The incident report if found.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IncidentReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetIncidentReportByIdQuery(id), cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
