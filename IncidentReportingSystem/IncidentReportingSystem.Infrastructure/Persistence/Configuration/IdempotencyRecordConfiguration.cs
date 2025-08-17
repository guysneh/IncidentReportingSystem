using IncidentReportingSystem.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core configuration for <see cref="IdempotencyRecord"/>.
    /// </summary>
    public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(200);
            builder.Property(x => x.PayloadHash).IsRequired().HasMaxLength(128);
            builder.Property(x => x.ResponseJson).IsRequired();
            builder.Property(x => x.ResponseContentType).IsRequired().HasMaxLength(100);

            // Uniqueness per key => First‑Write‑Wins semantics.
            builder.HasIndex(x => new { x.Key }).IsUnique();
            builder.HasIndex(x => x.ExpiresUtc);
        }
    }
}