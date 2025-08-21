using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Infrastructure.Persistence;
using Asp.Versioning;
using IncidentReportingSystem.Application.Features.Comments.Dtos;
using IncidentReportingSystem.Application.Features.Comments.Commands.Create;
using IncidentReportingSystem.Application.Exceptions;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Application.Features.Comments.Queries.ListComment;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.Application.Features.Comments.Commands.Delete;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// Versioned endpoints for comments that belong to incident reports.
    /// Base route: /api/v{version}/incidentreports/{incidentId}/comments
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/IncidentReports/{incidentId:guid}/comments")]
    public sealed class IncidentCommentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _db;

        public IncidentCommentsController(IMediator mediator, ApplicationDbContext db)
        {
            _mediator = mediator;
            _db = db;
        }

        /// <summary>
        /// List comments for an incident.
        /// Returns 404 if the incident does not exist.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PolicyNames.CanReadIncidents)] 
        [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListAsync(Guid incidentId, int skip = 0, int take = 50, CancellationToken ct = default)
        {
            var exists = await _db.IncidentReports.AsNoTracking().AnyAsync(x => x.Id == incidentId, ct);
            if (!exists) return NotFound();

            var result = await _mediator.Send(new ListCommentsQuery(incidentId, skip, Math.Clamp(take, 1, 100)), ct);
            return Ok(result);
        }

        /// <summary>
        /// Create a comment for an incident.
        /// 404 if incident missing, 403 if forbidden, 400 on validation errors.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanCommentOnIncident)]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateAsync(Guid incidentId, [FromBody] CreateCommentCommand body, CancellationToken ct = default)
        {
            try
            {
                var cmd = new CreateCommentCommand(incidentId,User.RequireUserId(), body.Text);
                var created = await _mediator.Send(cmd, ct);

                var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
                var location = $"/api/v{apiVersion}/incidentreports/{incidentId}/comments";
                return Created(location, created);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ForbiddenException ex)
            {
                return Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (FluentValidation.ValidationException vex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = vex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }

        }

        /// <summary>
        /// Delete a comment.
        /// 404 if not found (or belongs to a different incident), 403 if forbidden.
        /// </summary>
        [HttpDelete("{commentId:guid}")]
        [Authorize(Policy = PolicyNames.CanDeleteComment)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(Guid incidentId, Guid commentId, CancellationToken ct = default)
        {
            try
            {
                var isAdmin = User.IsInRole("Admin"); // adapt if your admin check differs
                await _mediator.Send(new DeleteCommentCommand(incidentId, commentId, User.RequireUserId(), isAdmin), ct);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                // If repository uses "stub delete" and no rows are affected → treat as 404 by contract.
                return NotFound();
            }
            catch (ForbiddenException ex)
            {
                return Problem(title: "Forbidden", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
        }
    }
}
