using IncidentReportingSystem.UI.Core.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http;
using System.Net.Http.Json;

namespace IncidentReportingSystem.UI.Core.Attachments;

public interface IAttachmentUploadService
{
    Task<AttachmentConstraintsVm> GetConstraintsAsync(CancellationToken ct = default);
    Task<StartUploadResponseVm> StartIncidentUploadAsync(string incidentId, string fileName, string contentType, CancellationToken ct = default);
    Task UploadToUrlAsync(string method, string url, IDictionary<string, string> headers, IBrowserFile file, long maxAllowed, CancellationToken ct = default);
    Task CompleteAsync(Guid attachmentId, CancellationToken ct = default);
    Task AbortAsync(Guid attachmentId, CancellationToken ct = default);
}

public sealed class AttachmentUploadService : IAttachmentUploadService
{
    private readonly HttpClient _raw;
    private readonly IApiClient _api;
    private readonly NavigationManager _nav;
    private readonly ILogger<AttachmentUploadService> _log;

    public AttachmentUploadService(HttpClient raw, IApiClient api, NavigationManager nav, ILogger<AttachmentUploadService> log)
    {
        _raw = raw;
        _api = api;
        _nav = nav;
        _log = log;
    }

    public Task<AttachmentConstraintsVm> GetConstraintsAsync(CancellationToken ct = default)
        => _api.GetJsonAsync<AttachmentConstraintsVm>("attachments/constraints", ct);

    public async Task<StartUploadResponseVm> StartIncidentUploadAsync(string incidentId, string fileName, string contentType, CancellationToken ct = default)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"incidentreports/{incidentId}/attachments/start")
        {
            Content = JsonContent.Create(new { fileName, contentType })
        };
        using var resp = await _api.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<StartUploadResponseVm>(cancellationToken: ct);
        return dto ?? throw new InvalidOperationException("Empty response from attachments/start.");
    }

    public async Task UploadToUrlAsync(
    string method,
    string url,
    IDictionary<string, string> headers,
    IBrowserFile file,
    long maxAllowed,
    CancellationToken ct = default)
    {
        await using var stream = file.OpenReadStream(maxAllowed, ct);
        using var content = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }

        var httpMethod = new HttpMethod(string.IsNullOrWhiteSpace(method) ? "PUT" : method);

        // Decide client: relative → API (loopback, needs auth); absolute → RAW (external presigned)
        var isAbsolute = Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri);
        var useApiClient = !isAbsolute || url.StartsWith("/", StringComparison.Ordinal);

        using var req = new HttpRequestMessage(httpMethod, isAbsolute ? absoluteUri! : new Uri(url, UriKind.Relative))
        {
            Content = content
        };

        // Copy headers required by storage / loopback endpoint
        foreach (var h in headers)
        {
            if (!req.Headers.TryAddWithoutValidation(h.Key, h.Value))
                req.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        HttpResponseMessage resp;
        if (useApiClient)
        {
            // Loopback upload to your API (needs auth handlers)
            resp = await _api.SendAsync(req, ct);
        }
        else
        {
            // External presigned URL (must be "clean", no auth handlers)
            resp = await _raw.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        }

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Upload failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }
    }



    public async Task CompleteAsync(Guid attachmentId, CancellationToken ct = default)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"attachments/{attachmentId}/complete");
        using var resp = await _api.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task AbortAsync(Guid attachmentId, CancellationToken ct = default)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post, $"attachments/{attachmentId}/abort");
        using var resp = await _api.SendAsync(msg, ct);
        resp.EnsureSuccessStatusCode();
    }
}
