namespace IncidentReportingSystem.Application.Common.Idempotency
{
    /// <summary>
    /// Persists idempotent responses keyed by a client-supplied <c>Idempotency-Key</c>
    /// and a stable hash of the payload. Implements First‑Write‑Wins per key.
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// Attempts to fetch a previously stored response for <paramref name="key"/>
        /// and <paramref name="payload"/>. Returns <c>null</c> if not found or expired.
        /// </summary>
        Task<TResponse?> TryGetAsync<TPayload, TResponse>(string key, TPayload payload, CancellationToken ct);

        /// <summary>
        /// Stores <paramref name="response"/> for <paramref name="key"/> and <paramref name="payload"/>,
        /// unless the key already exists. If the key exists, the originally stored response is returned
        /// unchanged (First‑Write‑Wins), ensuring idempotent behavior across retries.
        /// </summary>
        Task<TResponse> PutIfAbsentAsync<TPayload, TResponse>(string key, TPayload payload, TResponse response, TimeSpan ttl, CancellationToken ct);
    }
}