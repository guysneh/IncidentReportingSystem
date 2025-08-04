using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.Application.IncidentReports.Mappers;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReportById;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Domain.Enums;
using MediatR;

using Microsoft.AspNetCore.Authorization;
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
        /// <returns>The created incident report as DTO including its ID and metadata.</returns>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(IncidentReportDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateIncidentReportCommand command)
        {
            var result = await _mediator.Send(command).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result.ToDto());
        }

        /// <summary>
        /// Retrieves an incident report by its unique ID.
        /// </summary>
        /// <param name="id">The incident ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The incident report as DTO if found.</returns>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(IncidentReportDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetIncidentReportByIdQuery(id), cancellationToken).ConfigureAwait(false);
                return Ok(result.ToDto());
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a list of incident reports with optional filters.
        /// </summary>
        /// <param name="includeClosed">Include closed incidents.</param>
        /// <param name="skip">Number of items to skip (for paging).</param>
        /// <param name="take">Number of items to return (for paging).</param>
        /// <returns>List of incident reports as DTOs.</returns>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<IncidentReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] bool includeClosed = false,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50)
        {
            var query = new GetIncidentReportsQuery(includeClosed, skip, take);
            var results = await _mediator.Send(query).ConfigureAwait(false);
            return Ok(results.Select(r => r.ToDto()).ToList());
        }

        /// <summary>
        /// Updates the status of an existing incident report.
        /// </summary>
        /// <param name="id">ID of the incident report to update.</param>
        /// <param name="newStatus">New status to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content if update succeeded, or not found if ID doesn't exist.</returns>
        [Authorize]
        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] IncidentStatus newStatus, CancellationToken cancellationToken)
        {
            try
            {
                await _mediator.Send(new UpdateIncidentStatusCommand(id, newStatus), cancellationToken).ConfigureAwait(false);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
