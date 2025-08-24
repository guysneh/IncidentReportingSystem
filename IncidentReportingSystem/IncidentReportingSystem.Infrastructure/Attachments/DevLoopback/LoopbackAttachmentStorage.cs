using IncidentReportingSystem.Application.Abstractions.Attachments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace IncidentReportingSystem.Infrastructure.Attachments.DevLoopback
{
    /// <summary>
    /// Dev-only storage that returns a loopback API upload URL and keeps uploaded bytes in-memory.
    /// All guards/validation live here (controllers stay thin).
    /// </summary>
    public sealed class LoopbackAttachmentStorage : IAttachmentStorage
    {
        private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType, string ETag)> _store = new();

        private readonly string _publicBaseUrl;   // e.g. "https://localhost:7041" (no trailing slash)
        private readonly string _apiVersion;      // e.g. "v1"
        private readonly string _basePath;        // "/", "/api", "/irs", "/irs/api"

        public LoopbackAttachmentStorage(IConfiguration cfg)
        {
            _publicBaseUrl = (cfg["Api:PublicBaseUrl"] ?? throw new InvalidOperationException("Missing Api:PublicBaseUrl for Loopback storage.")).TrimEnd('/');
            _apiVersion = cfg["Api:DefaultVersion"] ?? "v1";
            _basePath = (cfg["Api:BasePath"] ?? "/").TrimEnd('/'); // "/", "/api", "/irs", "/irs/api"
        }

        public Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken ct)
        {
            var storagePath = $"{req.PathPrefix}/{req.AttachmentId}/{req.FileName}";
            storagePath = NormalizePath(storagePath);
            ValidateStoragePath(storagePath);

            // Build final URL honoring BasePath that may already include "/api"
            var basePart = _basePath == "/" ? "" : _basePath; // "" or "/api" or "/irs" or "/irs/api"
            var needsApi = !basePart.EndsWith("/api", StringComparison.OrdinalIgnoreCase);
            var apiSegment = needsApi ? "/api" : string.Empty;
            var uploadUrl = new Uri($"{_publicBaseUrl}{basePart}{apiSegment}/{_apiVersion}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}");

            return Task.FromResult(new CreateUploadSlotResult(storagePath, uploadUrl, DateTimeOffset.UtcNow.AddMinutes(10)));
        }

        public Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken ct)
        {
            storagePath = NormalizePath(storagePath);
            return Task.FromResult(_store.TryGetValue(storagePath, out var p)
                ? new UploadedBlobProps(p.Data.LongLength, p.ContentType, p.ETag)
                : null);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
        {
            storagePath = NormalizePath(storagePath);
            if (!_store.TryGetValue(storagePath, out var p))
                throw new FileNotFoundException(storagePath);

            return Task.FromResult<Stream>(new MemoryStream(p.Data, writable: false));
        }

        public Task DeleteAsync(string storagePath, CancellationToken ct)
        {
            storagePath = NormalizePath(storagePath);
            _store.TryRemove(storagePath, out _);
            return Task.CompletedTask;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path ?? string.Empty;
            // Decode once or twice to handle "%252F" -> "%2F" -> "/"
            var s = Uri.UnescapeDataString(path);
            if (s.Contains('%')) s = Uri.UnescapeDataString(s);
            return s;
        }

        private static void ValidateStoragePath(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new InvalidOperationException("Missing storage path.");

            // Must be *relative*
            if (storagePath.Contains("://", StringComparison.Ordinal) || storagePath.StartsWith('/'))
                throw new InvalidOperationException("Invalid storage path. Provide a relative path, not a full URL.");

            // Avoid traversal / wrong separators
            if (storagePath.Contains("..", StringComparison.Ordinal) || storagePath.Contains('\\'))
                throw new InvalidOperationException("Invalid storage path.");

            // Limit to known prefixes we generate
            if (!storagePath.StartsWith("incidents/", StringComparison.Ordinal) &&
                !storagePath.StartsWith("comments/", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid storage path prefix.");
        }

        private static string NormalizeContentType(string contentType, string storagePath)
        {
            // if client sent octet-stream or empty, infer from file extension
            if (string.IsNullOrWhiteSpace(contentType) ||
                contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(storagePath)?.ToLowerInvariant();
                return ext switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".pdf" => "application/pdf",
                    _ => "application/octet-stream"
                };
            }
            return contentType;
        }

        // PUT (binary)
        public async Task ReceiveUploadAsync(string storagePath, Stream body, string contentType, CancellationToken ct)
        {
            storagePath = NormalizePath(storagePath);
            ValidateStoragePath(storagePath);

            using var ms = new MemoryStream();
            await body.CopyToAsync(ms, ct).ConfigureAwait(false);
            var bytes = ms.ToArray();

            var normalizedCt = NormalizeContentType(contentType, storagePath);
            var etag = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            _store[storagePath] = (bytes, normalizedCt, etag);
        }

        // multipart (form)
        public async Task ReceiveUploadAsync(string storagePath, IFormFile file, CancellationToken ct)
        {
            storagePath = NormalizePath(storagePath);
            ValidateStoragePath(storagePath);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct).ConfigureAwait(false);
            var bytes = ms.ToArray();

            var incoming = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            var normalizedCt = NormalizeContentType(incoming, storagePath);
            var etag = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
            _store[storagePath] = (bytes, normalizedCt, etag);
        }
    }
}
