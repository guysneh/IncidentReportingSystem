namespace IncidentReportingSystem.API.Common
{
    /// <summary>Request body for starting an upload.</summary>
    public sealed class StartUploadBody
    {
        /// <summary>Original file name.</summary>
        public string FileName { get; set; } = null!;
        /// <summary>MIME content type.</summary>
        public string ContentType { get; set; } = null!;
    }
}
