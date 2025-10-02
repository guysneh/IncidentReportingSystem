using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace IncidentReportingSystem.UI.Core.Attachments
{
    public class AttachmentUploadService : IAttachmentUploadService
    {
        private readonly HttpClient _http;

        public AttachmentUploadService(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            // ודא ש-BaseAddress מוגדר ב-Program.cs (למשל ל-API)
            // _http.BaseAddress = new Uri("https://<api>/");
        }

        public Task<AttachmentConstraints> GetConstraintsAsync(CancellationToken ct)
            => _http.GetFromJsonAsync<AttachmentConstraints>("api/v1/attachments/constraints", ct)!;

        public async Task<UploadStartResponse> StartIncidentUploadAsync(
            Guid incidentId,
            string fileName,
            string contentType,
            long size,
            CancellationToken ct)
        {
            var payload = new StartUploadRequest
            {
                FileName = fileName,
                ContentType = contentType,
                Size = size
            };

            var resp = await _http.PostAsJsonAsync(
                $"api/v1/incidentreports/{incidentId}/attachments/start",
                payload,
                ct);

            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<UploadStartResponse>(cancellationToken: ct);
            if (dto == null) throw new InvalidOperationException("Empty StartUploadResponse.");
            return dto;
        }

        public async Task UploadToUrlAsync(
            string method,
            string url,
            IDictionary<string, string> headers,
            IBrowserFile file,
            long fileSize,
            CancellationToken ct)
        {
            using var req = new HttpRequestMessage(new HttpMethod(method ?? "PUT"), url);

            foreach (var kvp in headers ?? new Dictionary<string, string>())
            {
                // חלק מהכותרות חייבות להיות ב-Content (למשל Content-Type)
                if (string.Equals(kvp.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // נטפל בזה למטה דרך MediaTypeHeaderValue
                    continue;
                }
                req.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }

            var stream = file.OpenReadStream(maxAllowedSize: fileSize, cancellationToken: ct);
            var content = new StreamContent(stream);

            // אם הגיע Content-Type מהשרת – נשתמש בו, אחרת מהקובץ
            if (headers != null && headers.TryGetValue("Content-Type", out var ctHeader) && !string.IsNullOrWhiteSpace(ctHeader))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(ctHeader);
            }
            else if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            }

            content.Headers.ContentLength = fileSize;
            req.Content = content;

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task CompleteAsync(Guid attachmentId, CancellationToken ct)
        {
            var resp = await _http.PostAsync($"api/v1/attachments/{attachmentId}/complete", content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task AbortAsync(Guid attachmentId, CancellationToken ct)
        {
            var resp = await _http.PostAsync($"api/v1/attachments/{attachmentId}/abort", content: null, ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}
