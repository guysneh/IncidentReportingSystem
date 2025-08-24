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
            if (string.IsNullOrWhiteSpace(req.FileName)) throw new ArgumentException("FileName is required.");
            if (string.IsNullOrWhiteSpace(req.ContentType)) throw new ArgumentException("ContentType is required.");
            if (string.IsNullOrWhiteSpace(req.PathPrefix)) throw new ArgumentException("PathPrefix is required.");

            var prefix = NormalizePrefix(req.PathPrefix);
            var fileName = NormalizeFileName(req.FileName);
            var storagePath = $"{prefix}/{req.AttachmentId:D}/{fileName}";

            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            var blob = _container.GetBlobClient(storagePath);
            if (await blob.ExistsAsync(ct))
                throw new InvalidOperationException("Blob already exists at requested path.");

            var startsOn = DateTimeOffset.UtcNow.AddMinutes(-1);
            var expiresOn = DateTimeOffset.UtcNow.Add(_sasTtl);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _container.Name,
                BlobName = blob.Name,
                Resource = "b",
                StartsOn = startsOn,
                ExpiresOn = expiresOn,
                ContentType = req.ContentType
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write | BlobSasPermissions.Add);

            string sasQuery;
            if (_useSharedKey)
            {
                sasQuery = sasBuilder.ToSasQueryParameters(_sharedKey).ToString();
            }
            else
            {
                var udk = await _service.GetUserDelegationKeyAsync(startsOn, expiresOn, ct);
                sasQuery = sasBuilder.ToSasQueryParameters(udk.Value, _accountName).ToString();
            }

            // Client-facing URL uses PublicEndpoint if provided (dev), otherwise service endpoint (cloud).
            var baseEndpoint = _publicEndpoint ?? _service.Uri.ToString().TrimEnd('/');
            var uploadBase = $"{baseEndpoint}/{_container.Name}/{Uri.EscapeDataString(blob.Name)}";
            var uploadUri = new Uri($"{uploadBase}?{sasQuery}");

            return new CreateUploadSlotResult(storagePath, uploadUri, expiresOn);
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
