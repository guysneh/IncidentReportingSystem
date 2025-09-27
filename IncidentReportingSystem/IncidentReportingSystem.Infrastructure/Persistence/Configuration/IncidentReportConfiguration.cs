// File: IncidentReportingSystem.Infrastructure/Persistence/Configurations/IncidentReportConfiguration.cs
using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations;

public sealed class IncidentReportConfiguration : IEntityTypeConfiguration<IncidentReport>
{
    public void Configure(EntityTypeBuilder<IncidentReport> builder)
    {
        // Table
        builder.ToTable("incident_reports");

        // Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Core properties
        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Location)
            .HasMaxLength(256);

        builder.Property(x => x.ReporterId)
            .IsRequired()
            .HasMaxLength(64);

        // NEW: snapshot display name (never null; empty string if unavailable)
        builder.Property(x => x.ReporterDisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Category)
            .HasMaxLength(128);

        builder.Property(x => x.SystemAffected)
            .HasMaxLength(128);

        // Enums as strings (keep API/DB agree on string values)
        builder.Property(x => x.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        // Timestamps
        // Postgres: "timestamp with time zone" stores DateTimeOffset properly
        builder.Property(x => x.ReportedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // Useful indexes
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ReporterId);
        builder.HasIndex(x => new { x.Status, x.Severity });
    }
}
