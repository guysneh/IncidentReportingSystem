using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(320);

            b.Property(x => x.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(320);

            b.HasIndex(x => x.NormalizedEmail).IsUnique();

            // PostgreSQL text[]
            b.Property(x => x.Roles)
                .HasColumnType("text[]")
                .IsRequired();

            // bytea for hash/salt
            b.Property(x => x.PasswordHash)
                .HasColumnType("bytea")
                .IsRequired();

            b.Property(x => x.PasswordSalt)
                .HasColumnType("bytea")
                .IsRequired();

            b.Property(x => x.CreatedAtUtc)
                .HasColumnType("timestamp with time zone");
        }
    }
}
