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

namespace IncidentReportingSystem.API.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/incidents/{incidentId:guid}/comments")]
    [Produces(MediaTypeNames.Application.Json)]
    public sealed class IncidentCommentsController : ControllerBase
    {
        private const string Route_GetComments = "IncidentComments_List";
        private readonly IMediator _mediator;
        public IncidentCommentsController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [Authorize(Policy = PolicyNames.CanCommentOnIncident)]
        [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateAsync(Guid incidentId, [FromBody] CreateCommentRequest request, CancellationToken ct)
        {
            var authorId = GetUserIdOrThrow();
            var dto = await _mediator.Send(new CreateCommentCommand(incidentId, authorId, request.Text), ct);

            var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
            return CreatedAtRoute(Route_GetComments, new { incidentId, version }, dto);
        }

        [HttpGet(Name = Route_GetComments)]
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListAsync(Guid incidentId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
        {
            var list = await _mediator.Send(new GetCommentsQuery(incidentId, skip, take), ct);
            return Ok(list);
        }

        [HttpDelete("{commentId:guid}")]
        [Authorize(Policy = PolicyNames.CanDeleteComment)]
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

        public sealed record CreateCommentRequest(string Text);

        private Guid GetUserIdOrThrow()
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst("sub")?.Value;
            if (Guid.TryParse(sub, out var id) && id != Guid.Empty) return id;
            throw new UnauthorizedAccessException("Missing or invalid user id claim.");
        }
    }
}
