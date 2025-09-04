using IncidentReportingSystem.Application.Abstractions.Attachments;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IncidentReportingSystem.API.Health;

public sealed class AttachmentStorageHealthCheck : IHealthCheck
{
    private readonly IAttachmentStorage _storage;
    private readonly ILogger<AttachmentStorageHealthCheck> _logger;

    public AttachmentStorageHealthCheck(IAttachmentStorage storage, ILogger<AttachmentStorageHealthCheck> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Non-invasive probe: only asks storage to prepare an upload slot.
            var slot = await _storage.CreateUploadSlotAsync(
                new CreateUploadSlotRequest(Guid.NewGuid(), "health-probe.txt", "text/plain", "incidents/health-probe"),
                cancellationToken).ConfigureAwait(false);

            var data = new Dictionary<string, object?>
            {
                ["uploadPath"] = slot.UploadUrl.IsAbsoluteUri ? slot.UploadUrl.AbsolutePath : slot.UploadUrl.ToString(),
                ["storagePath"] = slot.StoragePath
            };

            return HealthCheckResult.Healthy("Attachment storage reachable.", data);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Attachment storage health probe failed.");
            return HealthCheckResult.Unhealthy("Attachment storage health probe failed.", ex);
        }
    }
}
