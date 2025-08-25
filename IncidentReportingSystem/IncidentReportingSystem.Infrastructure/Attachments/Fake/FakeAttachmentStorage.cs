using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;

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

        private readonly ConcurrentDictionary<string, Obj> _storage = new();

        public Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken cancellationToken)
        {
            var path = $"{req.PathPrefix}/{req.AttachmentId}/{req.FileName}";
            _storage[path] = new Obj { Bytes = Array.Empty<byte>(), ContentType = req.ContentType, ETag = Guid.NewGuid().ToString("N") };
            var expires = DateTimeOffset.UtcNow.AddMinutes(15);
            var url = new Uri($"https://fake-upload/{path}");
            return Task.FromResult(new CreateUploadSlotResult(path, url, expires));
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
    }
}
