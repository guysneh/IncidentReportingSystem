using IncidentReportingSystem.Application.Abstractions.Attachments;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Infrastructure.Attachments.Fake
{
    /// <summary>
    /// In-memory attachment storage for integration tests.
    /// Simulates client uploads deterministically without external dependencies.
    /// </summary>
    public sealed class FakeAttachmentStorage : IAttachmentStorage
    {
        private sealed class Obj
        {
            public byte[] Bytes = Array.Empty<byte>();
            public string ContentType = "application/octet-stream";
            public string ETag = Guid.NewGuid().ToString("N");
        }

        private const string UploadBase = "https://fake-upload/";

        private readonly ConcurrentDictionary<string, Obj> _storage = new();
        private static readonly Regex SafeFileName = new(@"^[\w\-. ]+$", RegexOptions.Compiled);

        public Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken ct)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.FileName)) throw new ArgumentException("FileName is required.", nameof(req));
            if (string.IsNullOrWhiteSpace(req.ContentType)) throw new ArgumentException("ContentType is required.", nameof(req));
            if (string.IsNullOrWhiteSpace(req.PathPrefix)) throw new ArgumentException("PathPrefix is required.", nameof(req));

            var prefix = NormalizePrefix(req.PathPrefix);     
            var fileName = NormalizeFileName(req.FileName);     
            var storagePath = $"{prefix}/{req.AttachmentId:D}/{fileName}";

            var uploadUrl = new Uri($"{UploadBase}upload?path={Uri.EscapeDataString(storagePath)}");
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-ms-blob-type"] = "BlockBlob"
            };

            return Task.FromResult(
                new CreateUploadSlotResult(
                    storagePath,
                    uploadUrl,
                    expiresAt,
                    method: "PUT",
                    headers
                )
            );
        }


        public Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken cancellationToken)
        {
            if (_storage.TryGetValue(storagePath, out var obj) && obj.Bytes.Length > 0)
                return Task.FromResult<UploadedBlobProps?>(new UploadedBlobProps(obj.Bytes.LongLength, obj.ContentType, obj.ETag));
            return Task.FromResult<UploadedBlobProps?>(null);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            if (!_storage.TryGetValue(storagePath, out var obj) || obj.Bytes.Length == 0)
                throw new FileNotFoundException("Object not found.");
            return Task.FromResult<Stream>(new MemoryStream(obj.Bytes, writable: false));
        }

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            _storage.TryRemove(storagePath, out _);
            return Task.CompletedTask;
        }

        /// <summary>Test-only helper to simulate client upload.</summary>
        public void SimulateClientUpload(string storagePath, byte[] bytes, string contentType)
        {
            _storage[storagePath] = new Obj { Bytes = bytes, ContentType = contentType, ETag = Guid.NewGuid().ToString("N") };
        }

        private static string NormalizePrefix(string pathPrefix)
        {
            var s = pathPrefix.Replace('\\', '/').Trim();
            while (s.StartsWith("/")) s = s[1..];
            while (s.EndsWith("/")) s = s[..^1];
            if (s.Contains("..", StringComparison.Ordinal)) throw new ArgumentException("Invalid PathPrefix.");
            if (s.Length == 0) throw new ArgumentException("Invalid PathPrefix.");
            return s;
        }

        private static string NormalizeFileName(string fileName)
        {
            var t = fileName.Trim();
            if (t.Length == 0) throw new ArgumentException("Invalid FileName.");
            if (!SafeFileName.IsMatch(t))
            {
                var sb = new StringBuilder(t.Length);
                foreach (var ch in t)
                    sb.Append(char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' or ' ' ? ch : '_');
                t = sb.ToString();
            }
            if (t is "." or "..") throw new ArgumentException("Invalid FileName.");
            return t;
        }
    }
}
