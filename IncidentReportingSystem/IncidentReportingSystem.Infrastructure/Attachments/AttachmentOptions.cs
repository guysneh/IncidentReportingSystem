using System.Collections.Generic;

namespace IncidentReportingSystem.Infrastructure.Attachments
{
    /// <summary>Options for attachment constraints. Bind from configuration section "Attachments".</summary>
    public sealed class AttachmentOptions
    {
        public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024;
        public int SasMinutesToLive { get; set; } = 15;
        public List<string> AllowedContentTypes { get; set; } = new()
        {
            "image/jpeg", "image/png", "application/pdf"
        };

        public List<string> AllowedExtensions { get; set; } = new()
        {
            ".jpg", ".jpeg", ".png", ".pdf"
        };
    }
}
