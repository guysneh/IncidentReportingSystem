using IncidentReportingSystem.Application.Abstractions.Attachments;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Tests.Application.Features.Attachments
{
    /// <summary>Minimal in-memory storage for unit tests. Not thread/process-safe across test runners.</summary>
    public sealed class InMemoryAttachmentStorage : IAttachmentStorage
    {
        private sealed record Blob(byte[] Data, string ContentType, string ETag);

        private readonly ConcurrentDictionary<string, Blob> _blobs = new(StringComparer.Ordinal);

        public Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken ct)
        {
            if (_blobs.TryGetValue(storagePath, out var b))
                return Task.FromResult<UploadedBlobProps?>(new UploadedBlobProps(b.Data.LongLength, b.ContentType, b.ETag));
            return Task.FromResult<UploadedBlobProps?>(null);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
        {
            if (!_blobs.TryGetValue(storagePath, out var b))
                throw new FileNotFoundException(storagePath);
            Stream ms = new MemoryStream(b.Data, writable: false);
            return Task.FromResult(ms);
        }

        public Task DeleteAsync(string storagePath, CancellationToken ct)
        {
            _blobs.TryRemove(storagePath, out _);
            return Task.CompletedTask;
        }

        public Task OverwriteAsync(string storagePath, Stream content, string contentType, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var etag = $"\"{ms.Length:x}-{DateTime.UtcNow.Ticks:x}\"";
            _blobs[storagePath] = new Blob(ms.ToArray(), contentType, etag);
            return Task.CompletedTask;
        }

        // Helper for tests
        public void Seed(string path, byte[] data, string contentType)
        {
            var etag = $"\"{data.LongLength:x}-{DateTime.UtcNow.Ticks:x}\"";
            _blobs[path] = new Blob(data, contentType, etag);
        }
    }
}
