using Asp.Versioning;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.API.Common;
using IncidentReportingSystem.API.Contracts.Paging;
using IncidentReportingSystem.Application.Abstractions.Logging;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.Application.Features.Attachments;
using IncidentReportingSystem.Application.Features.Attachments.Commands;
using IncidentReportingSystem.Application.Features.Attachments.Commands.AbortUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Commands.DeleteAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.Application.Features.Attachments.Queries;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentConstraints;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentMetedata;
using IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentStatus;
using IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent;
using IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Attachments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Security.Cryptography;
using System.Text;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// Versioned endpoints for starting uploads, completing uploads,
    /// retrieving metadata, and downloading attachments (including signed URLs).
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route(RouteConstants.Attachments)]
    [Authorize]
    [Tags("Attachments")]
    public sealed class AttachmentsController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ISignedUrlService _signedUrls;
        private readonly IAttachmentAuditService _audit;

        public AttachmentsController(ISender sender, ISignedUrlService signedUrls, IAttachmentAuditService audit)
        {
            _sender = sender;
            _signedUrls = signedUrls;
            _audit = audit;
        }

        /// <summary>Start an attachment upload for a specific incident.</summary>
        [HttpPost("~/" + RouteConstants.Incidents + "/{incidentId:guid}/attachments/start")]
        [ProducesResponseType(typeof(StartUploadAttachmentResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<StartUploadAttachmentResponse>> StartForIncident(
            Guid incidentId, [FromBody] StartUploadBody body, CancellationToken cancellationToken)
        {
            var res = await _sender.Send(new StartUploadAttachmentCommand(
                AttachmentParentType.Incident,
                incidentId, body.FileName, body.ContentType), cancellationToken).ConfigureAwait(false);
            return Ok(res);
        }

        /// <summary>Start an attachment upload for a specific comment.</summary>
        [HttpPost("~/" + RouteConstants.Comments + "/{commentId:guid}/attachments/start")]
        public async Task<ActionResult<StartUploadAttachmentResponse>> StartForComment(
            Guid commentId, [FromBody] StartUploadBody body, CancellationToken cancellationToken)
        {
            var res = await _sender.Send(new StartUploadAttachmentCommand(
                AttachmentParentType.Comment,
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
            await _sender.Send(new CompleteUploadAttachmentCommand(attachmentId), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>Get metadata for a specific attachment.</summary>
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

        /// <summary>
        /// Issues a temporary, signed download URL that can be used without authentication
        /// until the given TTL expires. Only completed attachments are eligible.
        /// </summary>
        /// <param name="attachmentId">Target attachment identifier.</param>
        /// <param name="ttlMinutes">Time-to-live in minutes for the signed URL (1–60). Default is 15.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// <c>200 OK</c> with <c>{ url, expiresAt }</c> where <c>url</c> is absolute and <c>expiresAt</c> is UTC.
        /// Returns <c>400</c> for invalid TTL, <c>409</c> if attachment is not completed, or <c>404</c> if missing.
        /// </returns>
        [HttpPost("{attachmentId:guid}/download-url")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSignedDownloadUrl(Guid attachmentId, [FromQuery] int ttlMinutes = 15, CancellationToken cancellationToken = default)
        {
            if (ttlMinutes < 1 || ttlMinutes > 60)
                return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["ttlMinutes"] = new[] { "ttlMinutes must be between 1 and 60." }
                }));

            var dto = await _sender.Send(new GetAttachmentMetadataQuery(attachmentId), cancellationToken).ConfigureAwait(false);
            if (dto.Status != AttachmentStatus.Completed)
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Attachment not completed.");

            var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
            var relative = Url.Action(
                action: nameof(Download),
                controller: "Attachments",
                values: new { version, attachmentId });

            var expUnix = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes).ToUnixTimeSeconds();
            var sig = _signedUrls.ComputeSignature(attachmentId, expUnix);

            // append query with interface-provided parameter names
            var relativeWithQuery = QueryHelpers.AddQueryString(relative!, new Dictionary<string, string>
            {
                [_signedUrls.ExpQueryName] = expUnix.ToString(),
                [_signedUrls.SigQueryName] = sig
            });

            var absolute = $"{Request.Scheme}://{Request.Host}{relativeWithQuery}";
            return Ok(new { url = absolute, expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix) });
        }

        /// <summary>
        /// Downloads attachment content. Works in two modes:
        /// 1) Authenticated: standard authorization applies (class-level [Authorize]).
        /// 2) Anonymous with a valid signed URL: pass 'exp' and 'sig' query parameters.
        /// </summary>
        /// <remarks>
        /// Signature format: sig = Base64Url(HMACSHA256(secret, $"{attachmentId:N}|{exp}")) where exp is Unix seconds (UTC).
        /// </remarks>
        [HttpGet("{attachmentId:guid}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken cancellationToken)
        {
            // Mode: auth or signed URL
            var isAuthenticated = User?.Identity?.IsAuthenticated == true;
            if (!isAuthenticated)
            {
                var expRaw = Request.Query[_signedUrls.ExpQueryName].FirstOrDefault();
                var sigRaw = Request.Query[_signedUrls.SigQueryName].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(expRaw) || string.IsNullOrWhiteSpace(sigRaw))
                    return Unauthorized(new ProblemDetails { Title = "Missing signed URL parameters." });

                if (!long.TryParse(expRaw, out var expUnix))
                    return Unauthorized(new ProblemDetails { Title = "Invalid expiration timestamp." });

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expUnix)
                    return Unauthorized(new ProblemDetails { Title = "Signed URL has expired." });

                if (!_signedUrls.IsValid(attachmentId, expUnix, sigRaw))
                    return Unauthorized(new ProblemDetails { Title = "Invalid signature." });
            }

            var meta = await _sender.Send(new GetAttachmentMetadataQuery(attachmentId), cancellationToken);
            if (meta is null || meta.Status != AttachmentStatus.Completed)
                return NotFound();

            var opened = await _sender.Send(new OpenAttachmentStreamQuery(attachmentId), cancellationToken);
            if (opened?.Stream is null) return NotFound();

            var normalizedEtag = NormalizeEtag(opened.ETag)
                ?? WeakEtagFrom(meta.Id, meta.Size ?? 0, meta.CompletedAt ?? meta.CreatedAt);
            var etagHeader = new EntityTagHeaderValue(normalizedEtag);

            var lastModified = opened.LastModifiedUtc ?? meta.CompletedAt ?? meta.CreatedAt;
            var fileName = opened.FileName ?? meta.FileName ?? "download.bin";
            var contentType = string.IsNullOrWhiteSpace(opened.ContentType) ? "application/octet-stream" : opened.ContentType;

            var reqEtags = Request.GetTypedHeaders().IfNoneMatch;
            if (reqEtags != null && reqEtags.Any(x => string.Equals(x.Tag.ToString(), etagHeader.Tag.ToString(), StringComparison.Ordinal)))
            {
                var th = Response.GetTypedHeaders();
                th.ETag = etagHeader;
                th.LastModified = lastModified;
                th.CacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromMinutes(5) };

                // audit: notModified=true
                _audit.AttachmentDownloaded(
                    attachmentId,
                    isAuthenticated ? "auth" : "signed",
                    notModified: true,
                    userId: isAuthenticated ? User.RequireUserId().ToString() : null);

                return StatusCode(StatusCodes.Status304NotModified);
            }

            var isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            var cd = new ContentDispositionHeaderValue(isImage ? "inline" : "attachment") { FileNameStar = fileName };
            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();

            // Cache headers (+ ETag)
            var headers = Response.GetTypedHeaders();
            headers.ETag = etagHeader;
            headers.LastModified = lastModified;
            headers.CacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromMinutes(5) };

            // audit: notModified=false
            _audit.AttachmentDownloaded(
                attachmentId,
                isAuthenticated ? "auth" : "signed",
                notModified: false,
                userId: isAuthenticated ? User.RequireUserId().ToString() : null);

            return File(
                fileStream: opened.Stream,
                contentType: contentType,
                fileDownloadName: null,
                lastModified: lastModified,
                entityTag: etagHeader,
                enableRangeProcessing: true
            );

            static string? NormalizeEtag(string? raw)
                => string.IsNullOrWhiteSpace(raw)
                    ? null
                    : (EntityTagHeaderValue.TryParse(raw, out _) ? raw : $"\"{raw}\"");

            static string WeakEtagFrom(Guid id, long size, DateTimeOffset stamp)
            {
                var material = Encoding.UTF8.GetBytes($"{id:N}|{size}|{stamp.UtcTicks}");
                using var sha = SHA256.Create();
                var hash = Convert.ToHexString(sha.ComputeHash(material)).ToLowerInvariant();
                return $"W/\"{hash}\"";
            }
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
                new ListAttachmentsByParentQuery(AttachmentParentType.Incident, incidentId, new AttachmentListFilters(Skip:skip, Take:take)),
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
                new ListAttachmentsByParentQuery(AttachmentParentType.Comment, commentId, new AttachmentListFilters(Skip: skip, Take: take)),
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
        /// Aborts a pending attachment upload. Only the original uploader or an Admin may abort.
        /// Pending uploads are deleted from storage (best-effort) and the record is removed.
        /// </summary>
        /// <response code="204">Upload aborted (or cleaned up) successfully.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Requester is not permitted to abort this upload.</response>
        /// <response code="404">Attachment not found.</response>
        /// <response code="409">Attachment is not pending (already completed/blocked).</response>
        [HttpPost("{attachmentId:guid}/abort")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Abort(Guid attachmentId, CancellationToken cancellationToken)
        {
            var userId = User.RequireUserId();              // from our existing auth helper
            var isAdmin = User.IsInRole("Admin");
            await _sender.Send(new AbortUploadAttachmentCommand(attachmentId, userId, isAdmin), cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        /// <summary>Get current status information of a specific attachment.</summary>
        [HttpGet("{attachmentId:guid}/status")]
        [ProducesResponseType(typeof(AttachmentStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AttachmentStatusDto>> GetStatus(Guid attachmentId, CancellationToken cancellationToken)
        {
            var dto = await _sender.Send(new GetAttachmentStatusQuery(attachmentId), cancellationToken).ConfigureAwait(false);
            return Ok(dto);
        }

       
        /// <summary>
        /// Lists attachments for an Incident. Supports search (q), content-type filter,
        /// date range, sorting (createdAt|fileName|size) and paging (skip/take).
        /// </summary>
        [HttpGet("incident-reports/{incidentId:guid}/attachments")]
        public async Task<IActionResult> ListByIncident(
            Guid incidentId,
            int skip = 0,
            int take = 100,
            string? q = null,
            string? contentType = null,
            DateTimeOffset? createdAfter = null,
            DateTimeOffset? createdBefore = null,
            string? orderBy = "createdAt",
            string? direction = "desc",
            CancellationToken ct = default)
        {
            var filters = new AttachmentListFilters(
                Search: q,
                ContentType: contentType,
                CreatedAfter: createdAfter,
                CreatedBefore: createdBefore,
                OrderBy: orderBy ?? "createdAt",
                Direction: direction ?? "desc",
                Skip: skip,
                Take: take);

            var result = await _sender.Send(
                new ListAttachmentsByParentQuery(AttachmentParentType.Incident, incidentId, filters), ct);

            return Ok(result);
        }

        /// <summary>
        /// Lists attachments for a Comment. Supports search (q), content-type filter,
        /// date range, sorting (createdAt|fileName|size) and paging (skip/take).
        /// </summary>
        [HttpGet("comments/{commentId:guid}/attachments")]
        public async Task<IActionResult> ListByComment(
            Guid commentId,
            int skip = 0,
            int take = 100,
            string? q = null,
            string? contentType = null,
            DateTimeOffset? createdAfter = null,
            DateTimeOffset? createdBefore = null,
            string? orderBy = "createdAt",
            string? direction = "desc",
            CancellationToken ct = default)
        {
            var filters = new AttachmentListFilters(
                Search: q,
                ContentType: contentType,
                CreatedAfter: createdAfter,
                CreatedBefore: createdBefore,
                OrderBy: orderBy ?? "createdAt",
                Direction: direction ?? "desc",
                Skip: skip,
                Take: take);

            var result = await _sender.Send(
                new ListAttachmentsByParentQuery(AttachmentParentType.Comment, commentId, filters), ct);

            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromServices] IAuthorizationService authz)
        {
            var auth = await authz.AuthorizeAsync(User, id, PolicyNames.AttachmentOwnerOnly);
            if (!auth.Succeeded) return Forbid();

            await _sender.Send(new DeleteAttachmentCommand(id));
            return NoContent();
        }
    }
}
