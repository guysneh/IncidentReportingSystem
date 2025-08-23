using System.Collections.Generic;

namespace IncidentReportingSystem.Infrastructure.Attachments
{
    /// <summary>Options for attachment constraints. Bind from configuration section "Attachments".</summary>
    public sealed class AttachmentOptions
    {
        public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
        public int SasMinutesToLive { get; set; } = 15;
        public IReadOnlyList<string> AllowedContentTypes { get; set; } =
            new[] { "image/png", "image/jpeg", "application/pdf" };
        public IReadOnlyList<string> AllowedExtensions { get; set; } =
            new[] { ".png", ".jpg", ".jpeg", ".pdf" };
    }
}
