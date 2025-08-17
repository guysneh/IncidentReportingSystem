using Asp.Versioning;
using IncidentReportingSystem.Application.IncidentReports.Commands.BulkUpdateIncidentStatus;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.Application.IncidentReports.Mappers;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReportById;
using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Domain.Auth;
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
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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
        [Authorize(Policy = PolicyNames.CanCreateIncident)]
        [HttpPost]
        [ProducesResponseType(typeof(IncidentReportDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateIncidentReportCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command,cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result.ToDto());
        }

        /// <summary>
        /// Retrieves an incident report by its unique ID.
        /// </summary>
        /// <param name="id">The incident ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The incident report as DTO if found.</returns>
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
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
        /// Retrieves a filtered list of incident reports with optional pagination and search criteria.
        /// </summary>
        /// <param name="status">Optional filter by incident category (e.g., Closed, InProgress).</param>
        /// <param name="skip">Number of incidents to skip for pagination.</param>
        /// <param name="take">Number of incidents to return for pagination (default is 50).</param>
        /// <param name="category">Optional filter by incident category (e.g., Electrical, Mechanical).</param>
        /// <param name="severity">Optional filter by incident severity (e.g., Low, Medium, High).</param>
        /// <param name="searchText">Optional text to search in description, location or reporter ID.</param>
        /// <param name="reportedAfter">Optional filter to include only incidents reported after this date.</param>
        /// <param name="reportedBefore">Optional filter to include only incidents reported before this date.</param>
        /// <param name="cancellationToken">Cancellation token for aborting the request.</param>
        /// <returns>A list of matching incident reports.</returns>

        /// <summary>
        /// Retrieves incident reports with filtering, paging and enum‑based sorting.
        /// Supported fields: <see cref="IncidentSortField"/>; direction: <see cref="SortDirection"/>.
        /// Default: <c>CreatedAt</c> <c>Desc</c> (newest first).
        /// </summary>
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<IncidentReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] IncidentStatus? status = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            [FromQuery] IncidentCategory? category = null,
            [FromQuery] IncidentSeverity? severity = null,
            [FromQuery] string? searchText = null,
            [FromQuery] DateTime? reportedAfter = null,
            [FromQuery] DateTime? reportedBefore = null,
            [FromQuery] IncidentSortField sortBy = IncidentSortField.CreatedAt,
            [FromQuery] SortDirection direction = SortDirection.Desc,
            CancellationToken cancellationToken = default)
        {
            if (take is < 1 or > 200) return BadRequest("take must be between 1 and 200.");
            if (skip < 0) return BadRequest("skip must be >= 0.");
            if (reportedAfter.HasValue && reportedBefore.HasValue && reportedAfter > reportedBefore)
                return BadRequest("reportedAfter must be <= reportedBefore.");

            var query = new GetIncidentReportsQuery(
                Status: status,
                Skip: skip,
                Take: take,
                Category: category,
                Severity: severity,
                SearchText: searchText,
                ReportedAfter: reportedAfter,
                ReportedBefore: reportedBefore,
                SortBy: sortBy,
                Direction: direction
            );

            var result = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Ok(result.Select(x => x.ToDto()));
        }


            /// <summary>
            /// Updates the status of an existing incident report.
            /// </summary>
            /// <param name="id">ID of the incident report to update.</param>
            /// <param name="newStatus">New status to apply.</param>
            /// <param name="cancellationToken">Cancellation token.</param>
            /// <returns>No content if update succeeded, or not found if ID doesn't exist.</returns>
            [Authorize(Policy = PolicyNames.CanManageIncidents)]
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

        /// <summary>
        /// Bulk-updates incident statuses. Requires the <c>Idempotency-Key</c> header.
        /// First‑Write‑Wins: the first response for a given key is returned for subsequent
        /// requests with the same key for 24h, even if the payload differs.
        /// </summary>
        [Authorize(Policy = PolicyNames.CanManageIncidents)]
        [HttpPost("bulk-status")]
        [ProducesResponseType(typeof(BulkStatusUpdateResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BulkStatus(
            [FromBody] BulkStatusUpdateRequest request,
            [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return BadRequest("Idempotency-Key header is required.");
            if (request.Ids is null || request.Ids.Count == 0)
                return BadRequest("Ids must be non-empty.");

            var cmd = new BulkUpdateIncidentStatusCommand(idempotencyKey, request.Ids, request.NewStatus);
            var result = await _mediator.Send(cmd, ct).ConfigureAwait(false);
            return Ok(result);
        }

        /// <summary>Request body for bulk status updates.</summary>
        public sealed record BulkStatusUpdateRequest(IReadOnlyList<Guid> Ids, IncidentStatus NewStatus);
    }
}
