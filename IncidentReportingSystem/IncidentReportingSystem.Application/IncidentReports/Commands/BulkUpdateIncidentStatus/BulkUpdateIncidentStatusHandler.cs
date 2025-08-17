using IncidentReportingSystem.Application.Common.Idempotency;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.BulkUpdateIncidentStatus
{
    /// <summary>
    /// Applies First‑Write‑Wins idempotency to bulk updates to prevent duplicate side‑effects under retries.
    /// </summary>
    public sealed class BulkUpdateIncidentStatusHandler : IRequestHandler<BulkUpdateIncidentStatusCommand, BulkStatusUpdateResultDto>
    {
        private readonly IIncidentReportRepository _repo;
        private readonly IIdempotencyStore _idemp;

        public BulkUpdateIncidentStatusHandler(IIncidentReportRepository repo, IIdempotencyStore idemp)
        {
            _repo = repo;
            _idemp = idemp;
        }

        /// <inheritdoc />
        public async Task<BulkStatusUpdateResultDto> Handle(BulkUpdateIncidentStatusCommand req, CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(req.IdempotencyKey);
            if (req.Ids is null || req.Ids.Count == 0)
                throw new ArgumentException("Ids must be non-empty.");

            // 1) Return a prior response for identical key+payload if present
            var cached = await _idemp.TryGetAsync<BulkPayload, BulkStatusUpdateResultDto>(
                req.IdempotencyKey, new BulkPayload(req.Ids, req.NewStatus), ct);
            if (cached is not null) return cached;

            // 2) Execute transactional bulk update
            var (updated, notFound) = await _repo.BulkUpdateStatusAsync(req.Ids, req.NewStatus, ct);
            var result = new BulkStatusUpdateResultDto
            {
                Updated = updated,
                NotFound = notFound,
                IdempotencyKey = req.IdempotencyKey
            };

            // 3) Persist the response for 24h (industry‑standard TTL)
            result = await _idemp.PutIfAbsentAsync(
                req.IdempotencyKey,
                new BulkPayload(req.Ids, req.NewStatus),
                result,
                TimeSpan.FromHours(24),
                ct);
            return result;
        }

        /// <summary>Internal payload representation used for stable hashing.</summary>
        private sealed record BulkPayload(IReadOnlyList<Guid> Ids, IncidentReportingSystem.Domain.Enums.IncidentStatus NewStatus);
    }
}
