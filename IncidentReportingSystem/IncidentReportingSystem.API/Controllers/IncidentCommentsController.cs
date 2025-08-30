using Asp.Versioning;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.API.Contracts.Paging;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Application.Features.Comments.Commands.Create;
using IncidentReportingSystem.Application.Features.Comments.Commands.Delete;
using IncidentReportingSystem.Application.Features.Comments.Dtos;
using IncidentReportingSystem.Application.Features.Comments.Queries.ListComment;
using IncidentReportingSystem.Application.Abstractions.Persistence; // <-- add
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// Versioned endpoints for comments that belong to incident reports.
    /// Base route: /api/v{version}/incidentreports/{incidentId}/comments
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/incidentreports/{incidentId:guid}/comments")]
    [Tags("Comments")]
    public sealed class IncidentCommentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIncidentCommentsRepository _comments; // <-- add

        public IncidentCommentsController(IMediator mediator, IIncidentCommentsRepository comments) // <-- ctor updated
        {
            _mediator = mediator;
            _comments = comments;
        }

        /// <summary>Lists comments for a given incident (paged, newest first).</summary>
        [HttpGet]
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [ProducesResponseType(typeof(PagedResponse<CommentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListAsync(Guid incidentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new ListCommentsQuery(incidentId, skip, take), cancellationToken).ConfigureAwait(false);

            var response = new PagedResponse<CommentDto>
            {
                Total = result.Total,
                Skip = result.Skip,
                Take = result.Take,
                Items = result.Items
            };

            return Ok(response);
        }

        /// <summary>Gets a single comment by composite key (incident + comment).</summary>
        [HttpGet("{commentId:guid}")]
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid incidentId, Guid commentId, CancellationToken cancellationToken = default)
        {
            var c = await _comments.GetAsync(incidentId, commentId, cancellationToken).ConfigureAwait(false);
            if (c is null) return NotFound();

            var dto = new CommentDto
            {
                Id = c.Id,
                IncidentId = c.IncidentId,
                UserId = c.UserId,
                Text = c.Text,
                CreatedAtUtc = c.CreatedAtUtc
            };

            return Ok(dto);
        }

        /// <summary>Creates a comment for an incident.</summary>
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
            var location = $"/api/v{apiVersion}/incidentreports/{incidentId}/comments/{created.Id}"; // <-- point to composite resource

            return Created(location, created);
        }

        /// <summary>Deletes a comment by composite key.</summary>
        [HttpDelete("{commentId:guid}")]
        [Authorize(Policy = PolicyNames.CanDeleteComment)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteAsync(Guid incidentId, Guid commentId, CancellationToken cancellationToken = default)
        {
            var isAdmin = User.IsInRole("Admin");
            await _mediator.Send(new DeleteCommentCommand(incidentId, commentId, User.RequireUserId(), isAdmin), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
    }
}
