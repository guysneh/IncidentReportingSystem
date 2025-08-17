using System.Text.Json;
using IncidentReportingSystem.Application.Common.Idempotency;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Services.Idempotency
{
    /// <summary>
    /// EF‑based idempotency store. Uses First‑Write‑Wins per idempotency key.
    /// </summary>
    public sealed class IdempotencyStoreEf : IIdempotencyStore
    {
        private readonly ApplicationDbContext _db;
        public IdempotencyStoreEf(ApplicationDbContext db) => _db = db;

        /// <inheritdoc />
        public async Task<TResponse?> TryGetAsync<TPayload, TResponse>(string key, TPayload payload, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var hash = PayloadHash.ComputeStableHash(payload);

            var rec = await _db.Set<IdempotencyRecord>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == key && r.PayloadHash == hash && r.ExpiresUtc > now, ct);

            if (rec is null) return default;
            return JsonSerializer.Deserialize<TResponse>(rec.ResponseJson)!;
        }

        /// <inheritdoc />
        public async Task<TResponse> PutIfAbsentAsync<TPayload, TResponse>(string key, TPayload payload, TResponse response, TimeSpan ttl, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var hash = PayloadHash.ComputeStableHash(payload);
            var json = JsonSerializer.Serialize(response);

            var existing = await _db.Set<IdempotencyRecord>().FirstOrDefaultAsync(r => r.Key == key, ct);
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
            await _db.SaveChangesAsync(ct);
            return response;
        }
    }
}