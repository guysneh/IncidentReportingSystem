using System.Net.Mime;
using System.Security.Claims;
using Asp.Versioning;
using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Comments.DTOs;
using IncidentReportingSystem.Application.Comments.Queries;
using IncidentReportingSystem.Domain.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// HTTP API for managing comments attached to incidents.
    /// Authorization is enforced globally; per-endpoint policies refine access.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/incidents/{incidentId:guid}/comments")]
    [Produces(MediaTypeNames.Application.Json)]
    public sealed class IncidentCommentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public IncidentCommentsController(IMediator mediator) => _mediator = mediator;

        /// <summary>Route name constants for link generation.</summary>
        private static class RouteNames
        {
            public const string GetIncidentComments = "GetIncidentComments";
        }

        /// <summary>Create a new comment on an incident.</summary>
        [HttpPost]
        [Authorize(Policy = PolicyNames.CanCommentOnIncident)] // ensure this policy exists (User or Admin)
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateAsync(Guid incidentId, [FromBody] CreateCommentRequest request, CancellationToken ct)
        {
            var authorId = GetUserIdOrThrow();
            var dto = await _mediator.Send(new CreateCommentCommand(incidentId, authorId, request.Text), ct);

            // Use a *named route* to generate the Location header deterministically.
            var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
            return CreatedAtRoute(
                routeName: RouteNames.GetIncidentComments,
                routeValues: new { version, incidentId },
                value: dto);
        }

        /// <summary>List comments on an incident (newest first).</summary>
        [HttpGet(Name = RouteNames.GetIncidentComments)]
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListAsync(
            Guid incidentId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            CancellationToken ct = default)
        {
            var list = await _mediator.Send(new GetCommentsQuery(incidentId, skip, take), ct);
            return Ok(list);
        }

        /// <summary>Delete a comment. Only the author or an Admin may delete.</summary>
        [HttpDelete("{commentId:guid}")]
        [Authorize(Policy = PolicyNames.CanDeleteComment)] // policy gates basic role; owner-or-admin enforced in handler
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(Guid incidentId, Guid commentId, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();
            var isAdmin = User?.IsInRole(Roles.Admin) ?? false;

            await _mediator.Send(new DeleteCommentCommand(incidentId, commentId, userId, isAdmin), ct);
            return NoContent();
        }

        /// <summary>Request body for creating a comment.</summary>
        public sealed record CreateCommentRequest(string Text);

        /// <summary>Extracts the current user id (GUID) from claims; throws if missing/invalid.</summary>
        private Guid GetUserIdOrThrow()
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst("sub")?.Value;
            if (Guid.TryParse(sub, out var id) && id != Guid.Empty) return id;
            throw new UnauthorizedAccessException("Missing or invalid user id claim.");
        }
    }
}
