using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace IncidentReportingSystem.Infrastructure.Attachments.DevLoopback
{
    /// <summary>
    /// Dev-only storage: persists files to local file-system under a safe root and exposes a loopback API upload URL.
    /// All validations happen here; controllers stay thin.
    /// </summary>
    public sealed class LoopbackAttachmentStorage : IAttachmentStorage
    {
        private readonly string _publicBaseUrl; // e.g. "https://localhost:7041" (no trailing slash)
        private readonly string _apiVersion;    // e.g. "v1"
        private readonly string _basePath;      // "/", "/api", "/irs", "/irs/api"
        private readonly string _root;          // local storage root
        private const string AllowedPrefix = "incidents/"; // keep in sync with tests

        public LoopbackAttachmentStorage(IConfiguration cfg)
        {
            // API address parts used only to compose uploadUrl returned from Start.
            _publicBaseUrl = (cfg["Api:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
            _apiVersion = string.IsNullOrWhiteSpace(cfg["Api:DefaultVersion"]) ? "v1" : cfg["Api:DefaultVersion"]!;
            _basePath = string.IsNullOrWhiteSpace(cfg["Api:BasePath"]) ? "/api" : cfg["Api:BasePath"]!;

            if (string.IsNullOrWhiteSpace(_publicBaseUrl))
                throw new InvalidOperationException("Missing Api:PublicBaseUrl for Loopback storage.");

            // File-system root (default to temp if not set)
            var configuredRoot = cfg["Attachments:Loopback:Root"];
            _root = string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(Path.GetTempPath(), "irs-loopback")
                : configuredRoot!;
            Directory.CreateDirectory(_root);
        }

        // ---------- Start -> upload slot ----------

        public Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken ct)
        {
            var storagePath = $"{req.PathPrefix}/{req.AttachmentId}/{req.FileName}";
            storagePath = NormalizePath(storagePath);
            ValidateStoragePath(storagePath);

            var basePart = _basePath == "/" ? "" : _basePath; // "" or "/api" or "/irs" or "/irs/api"
            var needsApi = !basePart.EndsWith("/api", StringComparison.OrdinalIgnoreCase);
            var apiSegment = needsApi ? "/api" : string.Empty;

            var uploadUrl = new Uri(
                $"{_publicBaseUrl}{basePart}{apiSegment}/{_apiVersion}/attachments/_loopback/upload?path={Uri.EscapeDataString(storagePath)}");

            // Loopback doesn't require any special headers; UI still gets a definitive method.
            var headers = (IReadOnlyDictionary<string, string>)new Dictionary<string, string>();

            return Task.FromResult(
                new CreateUploadSlotResult(
                    storagePath,
                    uploadUrl,
                    DateTimeOffset.UtcNow.AddMinutes(10),
                    method: "PUT",
                    headers: new Dictionary<string, string>() // loopback needs no special headers
                )
            );
        }

        // ---------- Upload endpoints write to disk ----------
        public async Task ReceiveUploadAsync(string relativePath, Stream body, string contentType, CancellationToken ct)
        {
            var full = CanonicalizeUnderRoot(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);

            if (File.Exists(full))
                throw new AttachmentAlreadyExistsException($"Object already exists at path '{relativePath}'.");

            await using var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await body.CopyToAsync(fs, 81920, ct);
            await fs.FlushAsync(ct);

            var normalized = NormalizeContentType(contentType, relativePath);
            await File.WriteAllTextAsync(full + ".contentType", normalized, ct);
        }

        public async Task ReceiveUploadAsync(string relativePath, IFormFile file, CancellationToken ct)
        {
            var full = CanonicalizeUnderRoot(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);

            if (File.Exists(full))
                throw new AttachmentAlreadyExistsException($"Object already exists at path '{relativePath}'.");

            await using var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await file.CopyToAsync(fs, ct);
            await fs.FlushAsync(ct);

            var normalized = NormalizeContentType(file.ContentType ?? "application/octet-stream", relativePath);
            await File.WriteAllTextAsync(full + ".contentType", normalized, ct);
        }

        // ---------- Query/Read/Delete over disk ----------
        public async Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken ct)
        {
            try
            {
                var fullPath = CanonicalizeUnderRoot(_root, storagePath);
                if (!File.Exists(fullPath))
                    return null;

                var metaPath = fullPath + ".contentType";
                string contentType = File.Exists(metaPath)
                    ? (await File.ReadAllTextAsync(metaPath, ct)).Trim()
                    : GuessContentType(fullPath);

                if (string.IsNullOrWhiteSpace(contentType))
                    contentType = GuessContentType(fullPath);

                var fi = new FileInfo(fullPath);
                var etag = ComputeETag(fi);
                return new UploadedBlobProps(fi.Length, contentType, etag);
            }
            catch (InvalidOperationException ex)
            {
                // Tests expect ArgumentException on invalid path for this method.
                throw new ArgumentException("Invalid storage path.", nameof(storagePath), ex);
            }
        }

        private static readonly Dictionary<string, string> _mime = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".pdf"] = "application/pdf"
        };

        private static string GuessContentType(string path)
        {
            var ext = Path.GetExtension(path);
            return _mime.TryGetValue(ext, out var ct) ? ct : "application/octet-stream";
        }

        private static string ComputeETag(FileInfo fi)
        {
            // simple, deterministic etag
            var len = fi.Length;
            var tics = fi.LastWriteTimeUtc.Ticks;
            return $"\"{len:x}-{tics:x}\"";
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
        {
            var full = CanonicalizeUnderRoot(_root, storagePath);
            if (!File.Exists(full))
                throw new FileNotFoundException(storagePath);

            Stream s = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            return Task.FromResult(s);
        }

        public Task DeleteAsync(string storagePath, CancellationToken ct)
        {
            var full = CanonicalizeUnderRoot(_root, storagePath);
            if (File.Exists(full)) File.Delete(full);
            var meta = full + ".contentType";
            if (File.Exists(meta)) File.Delete(meta);
            return Task.CompletedTask;
        }


        // ---------- Helpers ----------
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            var s = Uri.UnescapeDataString(path);
            if (s.Contains('%')) s = Uri.UnescapeDataString(s); // handle double-encoding
            s = s.Replace('\\', '/');
            return s;
        }

        private static void ValidateStoragePath(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new InvalidOperationException("Missing storage path.");

            if (storagePath.Contains("://", StringComparison.Ordinal) || storagePath.StartsWith('/'))
                throw new InvalidOperationException("Invalid storage path. Provide a relative path, not a full URL.");

            if (storagePath.Contains("..", StringComparison.Ordinal) || storagePath.Contains('\\'))
                throw new InvalidOperationException("Invalid storage path.");

            if (!storagePath.StartsWith("incidents/", StringComparison.Ordinal) &&
                !storagePath.StartsWith("comments/", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid storage path prefix.");
        }

        private static string NormalizeContentType(string contentType, string storagePath)
        {
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

        private static string CanonicalizeUnderRoot(string root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Invalid storage path.", nameof(relativePath));

            // Reject backslashes: tests require this to throw.
            if (relativePath.IndexOf('\\') >= 0)
                throw new InvalidOperationException("Invalid storage path.");

            // No leading slash and no absolute URL
            if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
                throw new InvalidOperationException("Invalid storage path.");
            if (Uri.TryCreate(relativePath, UriKind.Absolute, out _))
                throw new InvalidOperationException("Invalid storage path. Provide a relative path, not a full URL.");

            // No traversal
            if (relativePath.Contains("..", StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid storage path.");

            // Prefix whitelist (keep in sync with tests)
            var normalizedRel = relativePath.Replace('\\', '/');
            if (!normalizedRel.StartsWith("incidents/", StringComparison.OrdinalIgnoreCase) &&
                !normalizedRel.StartsWith("comments/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid storage path prefix.");

            // Resolve to a path under root
            var rootFull = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            var candidate = Path.GetFullPath(
                Path.Combine(rootFull, normalizedRel.Replace('/', Path.DirectorySeparatorChar)));

            if (!candidate.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Resolved path escapes storage root.");

            return candidate;
        }

    }
}
