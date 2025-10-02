using Microsoft.AspNetCore.Components.Forms;

namespace IncidentReportingSystem.UI.Core.Attachments
{
    public interface IAttachmentUploadService
    {
        Task UploadToUrlAsync(
            string method,
            string url,
            IDictionary<string, string> headers,
            IBrowserFile file,
            long fileSize,
            CancellationToken ct);
    }
}
