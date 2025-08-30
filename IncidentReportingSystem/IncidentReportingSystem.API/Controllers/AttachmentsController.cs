using Asp.Versioning;
using IncidentReportingSystem.API.Common;
using IncidentReportingSystem.API.Contracts.Paging;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.Application.Features.Attachments.Commands;
using IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.Application.Features.Attachments.Queries;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentConstraints;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentMetedata;
using IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent;
using IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Attachments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
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
    [Tags("Attachments")]
    public sealed class AttachmentsController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<AuthController> _logger;

        /// <summary>Creates a new <see cref="AttachmentsController"/>.</summary>
        public AttachmentsController(ISender sender, ILogger<AuthController> looger) 
        { 
            _sender = sender;
            _logger = looger; 
        }

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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete(Guid attachmentId, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginAuditScope(AuditTags.Attachments, AuditTags.Complete);
            await _sender.Send(new CompleteUploadAttachmentCommand(attachmentId), cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(AuditEvents.Attachments.Complete,
                "Attachment completed. AttachmentId={AttachmentId}", attachmentId);
            return NoContent();
        }

        /// <summary>Get metadata for a specific attachment.</summary>
        /// <response code="200">Attachment metadata returned.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not authorized to access this attachment.</response>
        /// <response code="404">Attachment not found.</response>
        [HttpGet("{attachmentId:guid}")]
        [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Metadata(Guid attachmentId, CancellationToken cancellationToken)
        {
            var dto = await _sender.Send(new GetAttachmentMetadataQuery(attachmentId), cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }

        /// <summary>Download attachment content (only for completed attachments).</summary>
        [HttpGet("{attachmentId:guid}/download")]
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken cancellationToken)
        {
            var resp = await _sender.Send(new OpenAttachmentStreamQuery(attachmentId), cancellationToken)
                                    .ConfigureAwait(false);

            // Conditional GET (If-None-Match)
            var reqEtags = Request.GetTypedHeaders().IfNoneMatch;
            if (reqEtags != null && reqEtags.Any(tag => tag.Tag == resp.ETag || tag.Tag == "*"))
            {
                Response.GetTypedHeaders().ETag = new EntityTagHeaderValue(resp.ETag);
                Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromMinutes(5) };
                return StatusCode(StatusCodes.Status304NotModified);
            }

            // Fresh response with caching
            var headers = Response.GetTypedHeaders();
            headers.ETag = new EntityTagHeaderValue(resp.ETag);
            headers.CacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromMinutes(5) };
            _logger.LogInformation(AuditEvents.Attachments.Download,
                "Attachment downloaded. AttachmentId={AttachmentId}", attachmentId);
            return File(resp.Stream, resp.ContentType, fileDownloadName: resp.FileName);
        }

        /// <summary>Get attachment constraints (allowed content types, max size, etc.).</summary>
        [HttpGet("constraints")]
        [AllowAnonymous]
        public IActionResult Get([FromServices] IOptions<AttachmentOptions> opts)
        {
            var o = opts.Value;
            return Ok(new
            {
                maxSizeBytes = o.MaxSizeBytes,
                allowedContentTypes = o.AllowedContentTypes,
                allowedExtensions = o.AllowedExtensions,
                uploadUrlTtlMinutes = o.SasMinutesToLive  
            });
        }

        /// <summary>
        /// Lists attachments for an incident, newest-first, with paging metadata.
        /// Route note: we keep existing naming convention 'incidentreports'.
        /// </summary>
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [HttpGet("~/api/v{version:apiVersion}/incidentreports/{incidentId:guid}/attachments")]
        public async Task<IActionResult> ListByIncident(
            Guid incidentId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var paged = await _sender.Send(
                new ListAttachmentsByParentQuery(AttachmentParentType.Incident, incidentId, skip, take),
                cancellationToken).ConfigureAwait(false);

            var response = new PagedResponse<AttachmentDto>
            {
                Total = paged.Total,
                Skip = paged.Skip,
                Take = paged.Take,
                Items = paged.Items
            };

            return Ok(response);
        }

        /// <summary>
        /// Lists attachments for a comment, newest-first, with paging metadata.
        /// </summary>
        [Authorize(Policy = PolicyNames.CanReadIncidents)]
        [HttpGet("~/api/v{version:apiVersion}/comments/{commentId:guid}/attachments")]
        public async Task<IActionResult> ListByComment(
            Guid commentId,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 100,
            CancellationToken cancellationToken = default)
        {
            var paged = await _sender.Send(
                new ListAttachmentsByParentQuery(AttachmentParentType.Comment, commentId, skip, take),
                cancellationToken).ConfigureAwait(false);

            var response = new PagedResponse<AttachmentDto>
            {
                Total = paged.Total,
                Skip = paged.Skip,
                Take = paged.Take,
                Items = paged.Items
            };

            return Ok(response);
        }
    }
}
