using Asp.Versioning;
using IncidentReportingSystem.API.Controllers.Models;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/attachments/_loopback")]
    [Authorize] 
    public sealed class AttachmentLoopbackUploadController : ControllerBase
    {
        private readonly LoopbackAttachmentStorage _loopback;
        public AttachmentLoopbackUploadController(LoopbackAttachmentStorage loopback) => _loopback = loopback;

        /// <summary>PUT binary upload (kept for completeness; works with the binary request-body filter)</summary>
        [HttpPut("upload")]
        [Consumes("application/octet-stream", "image/png", "image/jpeg", "application/pdf")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> UploadBinary([FromQuery] string path, CancellationToken ct)
        {
            var contentType = Request.ContentType ?? "application/octet-stream";
            await _loopback.ReceiveUploadAsync(path, Request.Body, contentType, ct).ConfigureAwait(false);
            return Created($"{Request.Path}?path={Uri.EscapeDataString(path)}", null);
        }

        /// <summary>
        /// Swagger-friendly multipart upload. Fill "path" (relative storage path) and choose a file.
        /// </summary>
        [HttpPost("upload-form")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> UploadForm([FromForm] LoopbackUploadForm form, CancellationToken ct)
        {
            await _loopback.ReceiveUploadAsync(form.Path, form.File, ct).ConfigureAwait(false);
            return Created($"{Request.Path}?path={Uri.EscapeDataString(form.Path)}", null);
        }
    }
}
