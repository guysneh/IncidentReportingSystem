using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentReportingSystem.UI.Core.Http;

namespace IncidentReportingSystem.UI.Core.Incidents
{
    public interface IIncidentService
    {
        Task<PagedResult<CommentDto>> GetCommentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default);
        Task<PagedResult<AttachmentDto>> GetIncidentAttachmentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default);
        Task<AttachmentDto?> GetAttachmentAsync(string attachmentId, CancellationToken ct = default);
        Task<string> GetAttachmentDownloadUrlAsync(string attachmentId, int ttlMinutes = 15, CancellationToken ct = default);
        Task<string> CreateIncidentAsync(CreateIncidentRequest req, CancellationToken ct = default);
        Task AddCommentAsync(string incidentId, string text, CancellationToken ct = default);
    }

    public sealed class IncidentService : IIncidentService
    {
        private readonly IApiClient _api;
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public IncidentService(IApiClient api) => _api = api;

        public Task<PagedResult<CommentDto>> GetCommentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default)
            => _api.GetJsonAsync<PagedResult<CommentDto>>($"incidentreports/{incidentId}/comments?skip={skip}&take={take}", ct);

        public Task<PagedResult<AttachmentDto>> GetIncidentAttachmentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default)
            => _api.GetJsonAsync<PagedResult<AttachmentDto>>($"incidentreports/{incidentId}/attachments?skip={skip}&take={take}", ct);

        public Task<AttachmentDto?> GetAttachmentAsync(string attachmentId, CancellationToken ct = default)
            => _api.GetJsonAsync<AttachmentDto?>($"attachments/{attachmentId}", ct);

        public async Task<string> GetAttachmentDownloadUrlAsync(string attachmentId, int ttlMinutes = 15, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"attachments/{attachmentId}/download-url?ttlMinutes={ttlMinutes}");
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            using var resp = await _api.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var media = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            var text = await resp.Content.ReadAsStringAsync(ct);

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Empty download-url response.");

            if (media == "application/json" || text.TrimStart().StartsWith("{"))
            {
                var obj = JsonSerializer.Deserialize<PresignedUrlResponse>(text, JsonOpts);
                if (string.IsNullOrWhiteSpace(obj?.Url)) throw new InvalidOperationException("Missing url field.");
                return obj!.Url!;
            }

            return text.Trim().Trim('"');
        }

        public async Task<string> CreateIncidentAsync(CreateIncidentRequest req, CancellationToken ct = default)
        {
            if (req.ReportedAt.HasValue)
            {
                var dt = req.ReportedAt.Value;
                if (dt.Kind == DateTimeKind.Unspecified)
                    dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);

                req.ReportedAt = dt.ToUniversalTime();
            }

            using var msg = new HttpRequestMessage(HttpMethod.Post, "IncidentReports")
            {
                Content = JsonContent.Create(req, options: JsonOpts)
            };
            msg.Headers.Accept.Clear();
            msg.Headers.Accept.ParseAdd("application/json");
            msg.Headers.Accept.ParseAdd("text/plain");

            using var resp = await _api.SendAsync(msg, ct);
            resp.EnsureSuccessStatusCode();

            var media = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            if ((media == "application/json" || body.TrimStart().StartsWith("{")) &&
                JsonSerializer.Deserialize<CreateIncidentResponse>(body, JsonOpts) is { Id: { } idFromJson } &&
                !string.IsNullOrWhiteSpace(idFromJson))
            {
                return idFromJson!;
            }

            return body.Trim().Trim('"');
        }


        public async Task AddCommentAsync(string incidentId, string text, CancellationToken ct = default)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, $"incidentreports/{incidentId}/comments")
            {
                Content = JsonContent.Create(new { text })
            };
            using var resp = await _api.SendAsync(msg, ct);
            resp.EnsureSuccessStatusCode();
        }

        private sealed class PresignedUrlResponse { public string? Url { get; set; } }
        private sealed class CreateIncidentResponse { public string? Id { get; set; } }
    }
}
