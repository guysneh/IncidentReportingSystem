using IncidentReportingSystem.Domain.Enums;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus
{
    /// <summary>
    /// Command to update the status of multiple incident reports atomically.
    /// Must be called via API with an <c>Idempotency-Key</c> header to ensure safe retries.
    /// </summary>
    public sealed record BulkUpdateIncidentStatusCommand(
        string IdempotencyKey,
        IReadOnlyList<Guid> Ids,
        IncidentStatus NewStatus
    ) : IRequest<BulkStatusUpdateResultDto>;

    /// <summary>
    /// Result for bulk status update operations.
    /// </summary>
    public sealed class BulkStatusUpdateResultDto
    {
        /// <summary>Total number of updated records.</summary>
        public int Updated { get; init; }
        /// <summary>Identifiers that were not found (no changes applied to them).</summary>
        public IReadOnlyList<Guid> NotFound { get; init; } = Array.Empty<Guid>();
        /// <summary>Echoes the idempotency key associated with this result.</summary>
        public string IdempotencyKey { get; init; } = string.Empty;
    }
}
