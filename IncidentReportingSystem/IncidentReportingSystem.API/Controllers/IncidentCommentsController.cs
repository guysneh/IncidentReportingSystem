using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Infrastructure.Persistence;
using Asp.Versioning;
using IncidentReportingSystem.Application.Features.Comments.Dtos;
using IncidentReportingSystem.Application.Features.Comments.Commands.Create;
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

        public IncidentCommentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// List comments for an incident.
        /// Returns 404 if the incident does not exist.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = PolicyNames.CanReadIncidents)] 
        [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListAsync(Guid incidentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new ListCommentsQuery(incidentId, skip, take), cancellationToken).ConfigureAwait(false) ;
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
        public async Task<IActionResult> CreateAsync(Guid incidentId, [FromBody] CreateCommentCommand body, CancellationToken cancellationToken = default)
        {
            var cmd = new CreateCommentCommand(incidentId, User.RequireUserId(), body.Text);
            var created = await _mediator.Send(cmd, cancellationToken).ConfigureAwait(false);

            var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
            var location = $"/api/v{apiVersion}/incidentreports/{incidentId}/comments";
            return Created(location, created);
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
        public async Task<IActionResult> DeleteAsync(Guid incidentId, Guid commentId, CancellationToken cancellationToken = default)
        {
            var isAdmin = User.IsInRole("Admin");
            await _mediator.Send(new DeleteCommentCommand(incidentId, commentId, User.RequireUserId(), isAdmin), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
    }
}
