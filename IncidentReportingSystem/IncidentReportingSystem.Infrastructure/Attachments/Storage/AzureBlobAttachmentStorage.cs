using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using Microsoft.WindowsAzure.Storage.Auth;

namespace IncidentReportingSystem.Infrastructure.Attachments.Storage
{
    /// <summary>
    /// Blob-backed implementation of IAttachmentStorage.
    /// - Local/CI/Compose: Azurite via Shared Key SAS (AccountName + AccountKey).
    /// - Cloud/Prod: Azure via Managed Identity (User Delegation SAS).
    /// All configuration comes from IConfiguration (environment).
    /// Optional PublicEndpoint is used only to *build* client-facing SAS URLs (dev),
    /// while the SDK still uses Endpoint internally.
    /// </summary>
    public sealed class AzureBlobAttachmentStorage : IAttachmentStorage
    {
        public sealed record Options(
            string Endpoint,            
            string AccountName,         
            string? AccountKey,         
            string Container,           
            TimeSpan UploadSasTtl,      
            string? PublicEndpoint      
        );

        private readonly BlobServiceClient _service;
        private readonly BlobContainerClient _container;
        private readonly bool _useSharedKey;
        private readonly StorageSharedKeyCredential? _sharedKey;
        private readonly string _accountName;
        private readonly TimeSpan _sasTtl;
        private readonly string? _publicEndpoint;

        private static readonly Regex SafeFileName = new(@"^[\w\-. ]+$", RegexOptions.Compiled);

        public AzureBlobAttachmentStorage(Options opts, Azure.Core.TokenCredential? msi = null)
        {
            if (string.IsNullOrWhiteSpace(opts.Endpoint)) throw new ArgumentException("Endpoint is required.", nameof(opts));
            if (string.IsNullOrWhiteSpace(opts.AccountName)) throw new ArgumentException("AccountName is required.", nameof(opts));
            if (string.IsNullOrWhiteSpace(opts.Container)) throw new ArgumentException("Container is required.", nameof(opts));
            if (opts.UploadSasTtl <= TimeSpan.Zero) throw new ArgumentException("UploadSasTtl must be positive.", nameof(opts));

            _accountName = opts.AccountName;
            _sasTtl = opts.UploadSasTtl;
            _useSharedKey = !string.IsNullOrWhiteSpace(opts.AccountKey);
            _publicEndpoint = string.IsNullOrWhiteSpace(opts.PublicEndpoint) ? null : opts.PublicEndpoint!.TrimEnd('/');

            var clientOptions = new BlobClientOptions(BlobClientOptions.ServiceVersion.V2021_12_02);

            if (_useSharedKey)
            {
                _sharedKey = new StorageSharedKeyCredential(opts.AccountName, opts.AccountKey!);
                var connectionString =
                    $"DefaultEndpointsProtocol=http;AccountName={opts.AccountName};AccountKey={opts.AccountKey};BlobEndpoint={opts.Endpoint};";
                _service = new BlobServiceClient(connectionString, clientOptions);
                _container = _service.GetBlobContainerClient(opts.Container);
            }
            else
            {
                var cred = msi ?? new DefaultAzureCredential();
                _service = new BlobServiceClient(new Uri(opts.Endpoint), cred, clientOptions);
                _container = _service.GetBlobContainerClient(opts.Container);
            }
        }

        public async Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken ct)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.FileName)) throw new ArgumentException("FileName is required.", nameof(req));
            if (string.IsNullOrWhiteSpace(req.ContentType)) throw new ArgumentException("ContentType is required.", nameof(req));
            if (string.IsNullOrWhiteSpace(req.PathPrefix)) throw new ArgumentException("PathPrefix is required.", nameof(req));

            // Normalize and compose the final relative blob path under the container
            var prefix = NormalizePrefix(req.PathPrefix);
            var fileName = NormalizeFileName(req.FileName);
            var storagePath = $"{prefix}/{req.AttachmentId:D}/{fileName}";

            // Ensure container exists (no public access)
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct).ConfigureAwait(false);

            var blob = _container.GetBlobClient(storagePath);
            if (await blob.ExistsAsync(ct).ConfigureAwait(false))
                throw new InvalidOperationException("Blob already exists at requested path.");

            // SAS time window
            var startsAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            var expiresAt = DateTimeOffset.UtcNow.Add(_sasTtl);

            // Build SAS for write (PUT) create
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _container.Name,
                BlobName = blob.Name,
                Resource = "b",
                StartsOn = startsAt,
                ExpiresOn = expiresAt,
                ContentType = req.ContentType
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write | BlobSasPermissions.Add);

            string sasQuery;
            if (_useSharedKey)
            {
                // Shared key flow
                sasQuery = sasBuilder.ToSasQueryParameters(_sharedKey).ToString();
            }
            else
            {
                // User Delegation Key (AAD) flow
                var udk = await _service.GetUserDelegationKeyAsync(startsAt, expiresAt, ct).ConfigureAwait(false);
                sasQuery = sasBuilder.ToSasQueryParameters(udk.Value, _accountName).ToString();
            }

            // Compose the client-facing upload URL.
            // If your class has a configured public endpoint, prefer it; otherwise use the blob primary URI.
            Uri uploadUrl;
            if (!string.IsNullOrWhiteSpace(_publicEndpoint))
            {
                var baseUri = _publicEndpoint.TrimEnd('/');
                var encodedPath = Uri.EscapeDataString(storagePath).Replace("%2F", "/"); // keep slashes
                uploadUrl = new Uri($"{baseUri}/{_container.Name}/{encodedPath}?{sasQuery}");
            }
            else
            {
                uploadUrl = new Uri($"{blob.Uri}?{sasQuery}");
            }

            // Extra headers the client must send with the PUT
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-ms-blob-type"] = "BlockBlob"
            };

            return new CreateUploadSlotResult(
                storagePath,
                uploadUrl,
                expiresAt,
                method: "PUT",
                headers
            );
        }

        public async Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("storagePath is required.", nameof(storagePath));
            var blob = _container.GetBlobClient(storagePath);
            try
            {
                var p = await blob.GetPropertiesAsync(cancellationToken: ct);
                return new UploadedBlobProps(p.Value.ContentLength,
                                             p.Value.ContentType ?? "application/octet-stream",
                                             p.Value.ETag.ToString().Trim('"'));
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("storagePath is required.", nameof(storagePath));
            var blob = _container.GetBlobClient(storagePath);
            var resp = await blob.DownloadStreamingAsync(cancellationToken: ct);
            return resp.Value.Content;
        }

        public async Task DeleteAsync(string storagePath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storagePath)) throw new ArgumentException("storagePath is required.", nameof(storagePath));
            var blob = _container.GetBlobClient(storagePath);
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
        }

        public async Task OverwriteAsync(string storagePath, Stream content, string contentType, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
                throw new ArgumentException("storagePath is required.", nameof(storagePath));

            var blob = _container.GetBlobClient(storagePath);

            // Upload will fully replace existing content; set content-type explicitly.
            var opts = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            content.Position = 0;
            await blob.UploadAsync(content, opts, ct).ConfigureAwait(false);
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
