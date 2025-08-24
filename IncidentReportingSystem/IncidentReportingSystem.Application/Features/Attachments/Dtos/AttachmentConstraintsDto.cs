using System;
using System.Collections.Generic;

namespace IncidentReportingSystem.Application.Features.Attachments.Dtos
{
    /// <summary>Represents client-facing attachment constraints.</summary>
    public sealed class AttachmentConstraintsDto
    {
        /// <summary>Maximum allowed size in bytes.</summary>
        public long MaxSizeBytes { get; init; }

        /// <summary>Allowed MIME content types (e.g., image/png).</summary>
        public IReadOnlyCollection<string> AllowedContentTypes { get; init; } = Array.Empty<string>();

        /// <summary>Allowed file extensions (e.g., .png, .pdf).</summary>
        public IReadOnlyCollection<string> AllowedExtensions { get; init; } = Array.Empty<string>();
    }
}
