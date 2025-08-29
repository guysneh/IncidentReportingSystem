using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity Framework Core configuration for <see cref="User"/>.
    /// Maps PostgreSQL-specific types (text[] for roles, bytea for password hash/salt),
    /// and configures optional profile fields with sensible length constraints.
    /// </summary>
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        /// <summary>
        /// Configures EF Core model mapping for the <see cref="User"/> entity.
        /// </summary>
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

            b.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired(false);

            b.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired(false);

            b.Property(x => x.DisplayName)
                .HasMaxLength(200)
                .IsRequired(false);

        }
    }
}
