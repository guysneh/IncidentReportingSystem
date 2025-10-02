using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace IncidentReportingSystem.UI.Core.Attachments
{
    public class AttachmentUploadService : IAttachmentUploadService
    {
        private readonly HttpClient _raw;          // קליינט "נקי" ללא Bearer/handlers
        private readonly Uri? _apiBaseUri;         // אופציונלי – אם תקבל כתובות יחסיות

        public AttachmentUploadService(HttpClient rawHttpClient, string? apiBaseUrl = null)
        {
            _raw = rawHttpClient ?? throw new ArgumentNullException(nameof(rawHttpClient));
            _apiBaseUri = string.IsNullOrWhiteSpace(apiBaseUrl) ? null : new Uri(apiBaseUrl!, UriKind.Absolute);
        }

        public async Task UploadToUrlAsync(
            string method,
            string url,
            IDictionary<string, string> headers,
            IBrowserFile file,
            long fileSize,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(method)) method = "PUT";
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("url is required", nameof(url));
            if (headers is null) throw new ArgumentNullException(nameof(headers));
            if (file is null) throw new ArgumentNullException(nameof(file));

            // 1) בונים URI (תומך גם בנתיב יחסי במקרה של loopback)
            Uri requestUri;
            if (Uri.TryCreate(url, UriKind.Absolute, out var abs))
            {
                requestUri = abs;
            }
            else
            {
                if (_apiBaseUri == null)
                    throw new InvalidOperationException("Relative URL provided but ApiBaseUrl was not configured.");
                requestUri = new Uri(_apiBaseUri, url);
            }

            using var req = new HttpRequestMessage(new HttpMethod(method), requestUri);

            // 2) תוכן + Content-Length (חשוב ל-Azure Blob; טוב גם ל-S3)
            // קלט מ-IBrowserFile:
            var stream = file.OpenReadStream(fileSize, ct); // מגביל לפי גודל שהעברנו
            HttpContent content;

            if (stream.CanSeek)
            {
                if (stream.Position != 0) stream.Position = 0;
                content = new StreamContent(stream);
                content.Headers.ContentLength = fileSize; // לא לשגר chunked
            }
            else
            {
                // fallback נדיר; BrowserFile בד"כ Seekable, אבל נשאיר הגיון מגן
                using var ms = new System.IO.MemoryStream();
                await stream.CopyToAsync(ms, ct);
                var bytes = ms.ToArray();
                content = new ByteArrayContent(bytes);
                content.Headers.ContentLength = bytes.LongLength;
            }

            // 3) Content-Type – נשלח רק אם קיים ב-headers החתומים/הנדרשים
            if (headers.TryGetValue("Content-Type", out var ctHeader) && !string.IsNullOrWhiteSpace(ctHeader))
                content.Headers.ContentType = new MediaTypeHeaderValue(ctHeader);

            req.Content = content;

            // 4) מעבירים רק את ה-Headers שהגיעו מהשרת (Presigned/SAS)
            foreach (var kv in headers)
            {
                var name = kv.Key;
                var value = kv.Value;

                if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue; // כבר הוגדר למעלה

                if (IsContentHeader(name))
                    req.Content.Headers.TryAddWithoutValidation(name, value);
                else
                    req.Headers.TryAddWithoutValidation(name, value);
            }

            // 5) טיפ ל-Azure Blob: אם כתובת blob וה-headers לא הכילו x-ms-blob-type
            if (requestUri.Host.Contains("blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                const string BlobType = "x-ms-blob-type";
                if (!req.Headers.Contains(BlobType) && !req.Content.Headers.Contains(BlobType))
                    req.Headers.TryAddWithoutValidation(BlobType, "BlockBlob");
            }

            // 6) לכבות Expect: 100-continue לפשט את הדרך
            req.Headers.ExpectContinue = false;

            // 7) שליחה
            using var res = await _raw.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Upload failed: {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
            }
        }

        private static bool IsContentHeader(string name) =>
            name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase);
    }
}
