using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentReportingSystem.Infrastructure.Persistence.Configurations
{
    /// <summary>EF Core configuration for <see cref="Attachment"/>.</summary>
    public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("attachments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ParentType).IsRequired();
            builder.Property(x => x.ParentId).IsRequired();

            builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(1024);

            builder.HasIndex(x => new { x.ParentType, x.ParentId });
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
