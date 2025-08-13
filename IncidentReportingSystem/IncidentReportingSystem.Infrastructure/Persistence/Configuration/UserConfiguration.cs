using IncidentReportingSystem.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping configuration for User entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(u => u.Id);

        b.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        b.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        // PostgreSQL text[] for multi-role support via Npgsql
        b.Property(u => u.Roles)
            .IsRequired()
            .HasColumnType("text[]");

        b.Property(u => u.PasswordHash)
            .IsRequired();

        b.Property(u => u.PasswordSalt)
            .IsRequired();

        b.Property(u => u.CreatedAtUtc)
            .IsRequired();

        b.HasIndex(u => u.NormalizedEmail)
            .IsUnique();
    }
}
