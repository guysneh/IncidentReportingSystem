using Asp.Versioning;
using IncidentReportingSystem.API.Common;
using IncidentReportingSystem.Application.Features.Attachments.Commands;
using IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.Application.Features.Attachments.Queries;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentConstraints;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentMetedata;
using IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// Versioned endpoints for starting uploads, completing uploads,
    /// retrieving metadata, and downloading attachments.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route(RouteConstants.Attachments)]
    [Authorize]
    public sealed class AttachmentsController : ControllerBase
    {
        private readonly ISender _sender;

        /// <summary>Creates a new <see cref="AttachmentsController"/>.</summary>
        public AttachmentsController(ISender sender) => _sender = sender;

        /// <summary>Start an attachment upload for a specific incident.</summary>
        [HttpPost("~/"+RouteConstants.Incidents+"/{incidentId:guid}/attachments/start")]
        public async Task<ActionResult<StartUploadAttachmentResponse>> StartForIncident(
            Guid incidentId, [FromBody] StartUploadBody body, CancellationToken cancellationToken)
        {
            var res = await _sender.Send(new StartUploadAttachmentCommand(
                Domain.Enums.AttachmentParentType.Incident,
                incidentId, body.FileName, body.ContentType), cancellationToken).ConfigureAwait(false);
            return Ok(res);
        }

        /// <summary>Start an attachment upload for a specific comment.</summary>
        [HttpPost("~/"+RouteConstants.Comments+"/{commentId:guid}/attachments/start")]
        public async Task<ActionResult<StartUploadAttachmentResponse>> StartForComment(
            Guid commentId, [FromBody] StartUploadBody body, CancellationToken cancellationToken)
        {
            var res = await _sender.Send(new StartUploadAttachmentCommand(
                Domain.Enums.AttachmentParentType.Comment,
                commentId, body.FileName, body.ContentType), cancellationToken).ConfigureAwait(false);
            return Ok(res);
        }

        /// <summary>Complete an upload by validating stored object and finalizing metadata.</summary>
        [HttpPost("{attachmentId:guid}/complete")]
        public async Task<IActionResult> Complete(Guid attachmentId, CancellationToken cancellationToken)
        {
            await _sender.Send(new CompleteUploadAttachmentCommand(attachmentId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>Get attachment metadata.</summary>
        [HttpGet("{attachmentId:guid}")]
        public async Task<IActionResult> Metadata(Guid attachmentId, CancellationToken cancellationToken)
        {
            var dto = await _sender.Send(new GetAttachmentMetadataQuery(attachmentId), cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }

        /// <summary>Download attachment content (only for completed attachments).</summary>
        [HttpGet("{attachmentId:guid}/download")]
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken cancellationToken)
        {
            var resp = await _sender.Send(new OpenAttachmentStreamQuery(attachmentId), cancellationToken).ConfigureAwait(false);
            return File(resp.Stream, resp.ContentType, fileDownloadName: resp.FileName);
        }

        /// <summary>Get attachment constraints (allowed content types, max size, etc.).</summary>
        [HttpGet("constraints")]
        [AllowAnonymous]
        public async Task<ActionResult<AttachmentConstraintsDto>> Constraints(CancellationToken cancellationToken)
        {
            var dto = await _sender.Send(new GetAttachmentConstraintsQuery(), cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }
    }
}
