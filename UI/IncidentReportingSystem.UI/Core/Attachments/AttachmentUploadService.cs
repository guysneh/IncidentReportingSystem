using IncidentReportingSystem.UI.Core.Http;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;

namespace IncidentReportingSystem.UI.Core.Attachments;

public class AttachmentUploadService : IAttachmentUploadService
{
    private readonly IApiClient _api; // זה ה-SecureApiClient דרך DI

    public AttachmentUploadService(IApiClient api)
    {
        _api = api;
    }

    public Task<AttachmentConstraints> GetConstraintsAsync(CancellationToken ct)
        => _api.GetJsonAsync<AttachmentConstraints>("attachments/constraints", ct);

    public sealed class StartUploadRequest
    {
        public string FileName { get; init; } = default!;
        public string ContentType { get; init; } = "application/octet-stream";
    }

    public async Task<UploadStartResponse> StartIncidentUploadAsync(
        Guid incidentId, string fileName, string contentType, CancellationToken ct)
    {
        var body = new StartUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
        };

        var path = $"incidentreports/{incidentId}/attachments/start";
        return await _api.PostJsonAsync<StartUploadRequest, UploadStartResponse>(path, body, ct);
    }
    public async Task UploadToUrlAsync(string method, string url, IDictionary<string, string> headers, IBrowserFile file, long fileSize, CancellationToken ct)
    {
        // אל תשלח Authorization כאן! זה הולך ל-SAS של Blob
        using var http = new HttpClient(); // לקוח נקי
        using var req = new HttpRequestMessage(new HttpMethod(method ?? "PUT"), url);

        foreach (var kv in headers ?? new Dictionary<string, string>())
        {
            if (string.Equals(kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        var stream = file.OpenReadStream(maxAllowedSize: fileSize, cancellationToken: ct);
        var content = new StreamContent(stream);
        var ctHeader = (headers != null && headers.TryGetValue("Content-Type", out var h) && !string.IsNullOrWhiteSpace(h))
            ? h : (file.ContentType ?? "application/octet-stream");

        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ctHeader);
        content.Headers.ContentLength = file.Size;
        req.Content = content;

        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
    }

    public Task CompleteAsync(Guid attachmentId, CancellationToken ct)
    => _api.PostJsonAsync<object>($"attachments/{attachmentId}/complete", new { }, ct);

    public Task AbortAsync(Guid attachmentId, CancellationToken ct)
        => _api.PostJsonAsync<object>($"attachments/{attachmentId}/abort", new { }, ct);
}

