using Asp.Versioning;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.API.Common;
using IncidentReportingSystem.API.Contracts.Paging;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.Application.Features.Attachments.Commands;
using IncidentReportingSystem.Application.Features.Attachments.Commands.AbortUploadAttachment;
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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
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
        private readonly ILogger<AttachmentsController> _logger;

        // Signing parameters for download URLs
        private readonly byte[] _signedUrlKey;
        
        // Query parameter names for signed URL scheme
        private const string SigParam = "sig";
        private const string ExpParam = "exp";

        /// <summary>
        /// Creates a new <see cref="AttachmentsController"/>.
        /// </summary>
        /// <param name="sender">Mediator used to dispatch application requests.</param>
        /// <param name="configuration">
        /// Configuration used to resolve a signing secret for temporary download URLs.
        /// Uses "Attachments:DownloadUrlSecret" when present; falls back to "Jwt:Secret".
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if no signing secret is configured.</exception>
        public AttachmentsController(ISender sender, IConfiguration configuration, ILogger<AttachmentsController> logger)
        {
            _sender = sender;

            var secret = configuration["Attachments:DownloadUrlSecret"] ?? configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Missing signing secret for signed attachment URLs. Provide 'Attachments:DownloadUrlSecret' or 'Jwt:Secret'.");

            _signedUrlKey = Encoding.UTF8.GetBytes(secret);
            _logger = logger;
        }

        /// <summary>Start an attachment upload for a specific incident.</summary>
        [HttpPost("~/" + RouteConstants.Incidents + "/{incidentId:guid}/attachments/start")]
        [ProducesResponseType(typeof(StartUploadAttachmentResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<StartUploadAttachmentResponse>> StartForIncident(
            Guid incidentId, [FromBody] StartUploadBody body, CancellationToken cancellationToken)
        {
            var res = await _sender.Send(new StartUploadAttachmentCommand(
                Domain.Enums.AttachmentParentType.Incident,
                incidentId, body.FileName, body.ContentType), cancellationToken).ConfigureAwait(false);
            return Ok(res);
        }

        /// <summary>Start an attachment upload for a specific comment.</summary>
        [HttpPost("~/" + RouteConstants.Comments + "/{commentId:guid}/attachments/start")]
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
            await _sender.Send(new CompleteUploadAttachmentCommand(attachmentId), cancellationToken).ConfigureAwait(false);
            // --- AUDIT: attachments.complete ---
            var userId = User?.Claims?.FirstOrDefault(c =>
                             c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value;
            _logger.LogInformation(
                AuditEvents.Attachments.Complete,
                "Attachment completed {tags} {AttachmentId} {UserId}",
                "attachments,complete",
                attachmentId,
                userId ?? string.Empty);
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
            var exp = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes).ToUnixTimeSeconds();
            var sig = ComputeSignature(attachmentId, exp);

            var relative = Url.Action(
                action: nameof(Download),
                controller: "Attachments",
                values: new { version, attachmentId, exp, sig });

            var absolute = $"{Request.Scheme}://{Request.Host}{relative}";
            return Ok(new { url = absolute, expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp) });
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
        [AllowAnonymous] // accept anonymous when a valid signed URL is provided
        public async Task<IActionResult> Download(Guid attachmentId, CancellationToken cancellationToken)
        {
            // If caller is not authenticated, validate signed URL parameters.
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                var expStr = Request.Query[ExpParam].FirstOrDefault();
                var sigStr = Request.Query[SigParam].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(expStr) || string.IsNullOrWhiteSpace(sigStr))
                    return Unauthorized(new ProblemDetails { Title = "Missing signed URL parameters." });

                if (!long.TryParse(expStr, out var exp))
                    return Unauthorized(new ProblemDetails { Title = "Invalid expiration timestamp." });

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                    return Unauthorized(new ProblemDetails { Title = "Signed URL has expired." });

                if (!IsSignatureValid(attachmentId, exp, sigStr))
                    return Unauthorized(new ProblemDetails { Title = "Invalid signature." });
            }

            var resp = await _sender.Send(new OpenAttachmentStreamQuery(attachmentId), cancellationToken)
                            .ConfigureAwait(false);

            // Build typed headers
            var etagValue = string.IsNullOrWhiteSpace(resp.ETag) ? null : new EntityTagHeaderValue(resp.ETag);

            // Conditional GET: If-None-Match → 304 (Not Modified)
            var reqHeaders = Request.GetTypedHeaders();
            if (etagValue != null && reqHeaders.IfNoneMatch != null && reqHeaders.IfNoneMatch.Any(tag => tag.Tag == etagValue.Tag))
            {
                var r = Response.GetTypedHeaders();
                r.ETag = etagValue;
                r.CacheControl = new CacheControlHeaderValue
                {
                    Private = true,
                    MaxAge = TimeSpan.FromMinutes(5)
                };

                // Audit (download attempt that resulted in 304 is still an access event)
                var isAuthed304 = User?.Identity?.IsAuthenticated == true;
                var userId304 = isAuthed304
                    ? User!.Claims!.FirstOrDefault(c =>
                        c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value
                    : null;

                _logger.LogInformation(
                    AuditEvents.Attachments.Download,
                    "Attachment download {tags} {AttachmentId} {Mode} {UserId}",
                    "attachments,download",
                    attachmentId,
                    isAuthed304 ? "auth" : "signed",
                    userId304 ?? string.Empty);

                return StatusCode(StatusCodes.Status304NotModified);
            }

            // 200: set ETag + Cache-Control
            var headers = Response.GetTypedHeaders();
            if (etagValue != null)
                headers.ETag = etagValue;

            headers.CacheControl = new CacheControlHeaderValue
            {
                Private = true,
                MaxAge = TimeSpan.FromMinutes(5)
            };

            // Existing audit log for download (200 path)
            var isAuthed = User?.Identity?.IsAuthenticated == true;
            var userId = isAuthed
                ? User!.Claims!.FirstOrDefault(c =>
                    c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub")?.Value
                : null;

            _logger.LogInformation(
                AuditEvents.Attachments.Download,
                "Attachment download {tags} {AttachmentId} {Mode} {UserId}",
                "attachments,download",
                attachmentId,
                isAuthed ? "auth" : "signed",
                userId ?? string.Empty);

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

        // ---------- Signed URL helpers ----------

        /// <summary>
        /// Computes a Base64Url-encoded HMAC-SHA256 signature over "{attachmentId:N}|{exp}".
        /// </summary>
        private string ComputeSignature(Guid attachmentId, long expUnixSeconds)
        {
            var payload = $"{attachmentId:N}|{expUnixSeconds}";
            using var hmac = new HMACSHA256(_signedUrlKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return WebEncoders.Base64UrlEncode(hash);
        }

        /// <summary>
        /// Validates a Base64Url-encoded HMAC-SHA256 signature and protects against timing attacks.
        /// </summary>
        private bool IsSignatureValid(Guid attachmentId, long expUnixSeconds, string providedSig)
        {
            byte[] provided;
            try
            {
                provided = WebEncoders.Base64UrlDecode(providedSig);
            }
            catch
            {
                return false;
            }

            var payload = $"{attachmentId:N}|{expUnixSeconds}";
            using var hmac = new HMACSHA256(_signedUrlKey);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

            if (provided.Length != expected.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(provided, expected);
        }
    }
}
