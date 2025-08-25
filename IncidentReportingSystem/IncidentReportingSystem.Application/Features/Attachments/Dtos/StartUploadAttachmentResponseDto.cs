using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Dtos
{
    /// <summary>
    /// Start-upload response returned to the client. It includes a dev-friendly <see cref="StoragePath"/>
    /// for Swagger testing with the loopback uploader.
    /// </summary>
    public sealed record StartUploadAttachmentResponseDto(
        Guid AttachmentId,
        Uri UploadUrl,
        string StoragePath
    );
}
