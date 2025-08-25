namespace IncidentReportingSystem.API.Controllers.Models
{
    /// <summary>Multipart form used by the loopback upload endpoint in Swagger.</summary>
    public sealed class LoopbackUploadForm
    {
        /// <summary>Relative storage path (e.g. "incidents/{incidentId}/{attachmentId}/file.ext").</summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>File to upload.</summary>
        public IFormFile File { get; set; } = default!;
    }
}
