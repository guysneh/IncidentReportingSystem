using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core mapping for <see cref="IncidentComment"/>.</summary>
    public sealed class IncidentCommentConfiguration : IEntityTypeConfiguration<IncidentComment>
    {
        public void Configure(EntityTypeBuilder<IncidentComment> builder)
        {
            builder.ToTable("incident_comments", schema: "public");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.IncidentId).IsRequired();
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.HasIndex(x => x.IncidentId);
            builder.HasIndex(x => new { x.IncidentId, x.CreatedAtUtc });

            builder.HasOne(x => x.Incident)
                   .WithMany()
                   .HasForeignKey(x => x.IncidentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}