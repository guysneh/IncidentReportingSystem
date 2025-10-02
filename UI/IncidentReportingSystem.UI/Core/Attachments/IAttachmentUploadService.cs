using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace IncidentReportingSystem.UI.Core.Attachments
{
    public interface IAttachmentUploadService
    {
        // שלב 1: קבלת מגבלות מהשרת
        Task<AttachmentConstraints> GetConstraintsAsync(CancellationToken ct);

        // שלב 2: התחלת העלאה עבור Incident
        Task<UploadStartResponse> StartIncidentUploadAsync(
            Guid incidentId,
            string fileName,
            string contentType,
            long size,
            CancellationToken ct);

        // שלב 3: העלאה בפועל ל-URL חיצוני/לופבאק
        Task UploadToUrlAsync(
            string method,
            string url,
            IDictionary<string, string> headers,
            IBrowserFile file,
            long fileSize,
            CancellationToken ct);

        // שלב 4: השלמה/ביטול
        Task CompleteAsync(Guid attachmentId, CancellationToken ct);
        Task AbortAsync(Guid attachmentId, CancellationToken ct);
    }
}
