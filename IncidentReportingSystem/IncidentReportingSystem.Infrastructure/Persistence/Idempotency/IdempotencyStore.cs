using System.Text.Json;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Persistence.Idempotency
{
    /// <summary>
    /// EF‑based idempotency store. Uses First‑Write‑Wins per idempotency key.
    /// </summary>
    public sealed class IdempotencyStore : IIdempotencyStore
    {
        private readonly ApplicationDbContext _db;
        public IdempotencyStore(ApplicationDbContext db) => _db = db;

        /// <inheritdoc />
        public async Task<TResponse?> TryGetAsync<TPayload, TResponse>(string key, TPayload payload, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var hash = PayloadHash.ComputeStableHash(payload);

            var rec = await _db.Set<IdempotencyRecord>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == key && r.PayloadHash == hash && r.ExpiresUtc > now, cancellationToken).ConfigureAwait(false);

            if (rec is null) return default;
            return JsonSerializer.Deserialize<TResponse>(rec.ResponseJson)!;
        }

        /// <inheritdoc />
        public async Task<TResponse> PutIfAbsentAsync<TPayload, TResponse>(string key, TPayload payload, TResponse response, TimeSpan ttl, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var hash = PayloadHash.ComputeStableHash(payload);
            var json = JsonSerializer.Serialize(response);

            var existing = await _db.Set<IdempotencyRecord>().FirstOrDefaultAsync(r => r.Key == key, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                return JsonSerializer.Deserialize<TResponse>(existing.ResponseJson)!; // First‑Write‑Wins
            }

            var rec = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                Key = key,
                PayloadHash = hash,
                ResponseJson = json,
                ResponseContentType = "application/json",
                CreatedUtc = now,
                ExpiresUtc = now.Add(ttl)
            };

            _db.Add(rec);
            await _db.SaveChangesAsync(cancellationToken);
            return response;
        }
    }
}