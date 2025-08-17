namespace IncidentReportingSystem.Infrastructure.Persistence.Idempotency
{
    /// <summary>
    /// EF entity capturing the stored response for an idempotent request.
    /// </summary>
    public class IdempotencyRecord
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; }
        /// <summary>Client‑supplied idempotency key.</summary>
        public string Key { get; set; } = default!;
        /// <summary>Stable SHA‑256 (hex) of the JSON payload.</summary>
        public string PayloadHash { get; set; } = default!;
        /// <summary>Serialized response JSON.</summary>
        public string ResponseJson { get; set; } = default!;
        /// <summary>MIME of the serialized response (defaults to application/json).</summary>
        public string ResponseContentType { get; set; } = "application/json";
        /// <summary>UTC creation timestamp.</summary>
        public DateTime CreatedUtc { get; set; }
        /// <summary>UTC expiration timestamp (record ignored/eligible for pruning after this time).</summary>
        public DateTime ExpiresUtc { get; set; }
    }
}