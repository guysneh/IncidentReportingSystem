using FluentValidation;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    /// <summary>
    /// Test seeder:
    /// - Creates Incidents/Comments via the real MediatR commands (so domain rules apply).
    /// - Starts attachments via the real Start command (normalized to PDF to pass validators).
    /// - Updates DB columns (size/status/completedAt) directly via EF metadata, to avoid
    ///   private setters and storage-side requirements during tests.
    /// </summary>
    public sealed class TestSeeder
    {
        private readonly IServiceProvider _sp;
        public TestSeeder(IServiceProvider sp) => _sp = sp;

        /// <summary>
        /// Creates an Incident using the real CreateIncident handler.
        /// Adjust the command type/parameters if your project differs.
        /// </summary>
        public async Task<Guid> CreateIncidentAsync(
            string description = "seed incident",
            string location = "Test-Location",
            string reporterId = "it-user")
        {
            using var scope = _sp.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var cmd = new IncidentReportingSystem.Application.Features.IncidentReports.Commands.CreateIncidentReport.CreateIncidentReportCommand(
                description: description,
                location: location,
                reporterId: Guid.NewGuid(),
                category: IncidentCategory.PowerOutage,
                systemAffected: "N/A",
                severity: IncidentSeverity.Low
            );

            var incidentId = await mediator.Send(cmd);
            return incidentId.Id;
        }

        /// <summary>
        /// Creates a Comment for an Incident using the real handler.
        /// </summary>
        public async Task<Guid> CreateCommentAsync(Guid incidentId, string text = "seed comment")
        {
            using var scope = _sp.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var cmd = new IncidentReportingSystem.Application.Features.Comments.Commands.Create.CreateCommentCommand(
                AuthorId: Guid.NewGuid(),
                IncidentId: incidentId,
                Text: text
            );

            var commentId = await mediator.Send(cmd);
            return commentId.Id;
        }

        /// <summary>
        /// Starts an attachment. We normalize filename/content-type to PDF to satisfy validators.
        /// Note: StartUploadAttachmentCommand does NOT accept 'Size'; we set size later in DB.
        /// </summary>
        public async Task<Guid> StartAttachmentAsync(
            AttachmentParentType parentType,
            Guid parentId,
            string fileName,
            string contentType,
            long size)
        {
            using var scope = _sp.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var normalizedFileName = $"{baseName}.pdf";
            const string normalizedContentType = "application/pdf";

            var cmd = new IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment.StartUploadAttachmentCommand(
                ParentType: parentType,
                ParentId: parentId,
                FileName: normalizedFileName,
                ContentType: normalizedContentType
            );

            var startResult = await mediator.Send(cmd);

            await ForceSetAttachmentColumnsAsync(
                id: startResult.AttachmentId,
                size: size,
                fileName: fileName,                
                contentType: contentType           
            );

            return startResult.AttachmentId;
        }


        /// <summary>
        /// Marks attachment as Completed directly in DB (bypasses storage checks).
        /// </summary>
        public async Task CompleteAttachmentAsync(Guid attachmentId, string? checksumSha256 = null)
        {
            var now = DateTimeOffset.UtcNow;
            await ForceSetAttachmentColumnsAsync(
                id: attachmentId,
                status: AttachmentStatus.Completed,
                completedAt: now
            );
        }

        /// <summary>
        /// Generic low-level column updater for Attachments using EF Core relational metadata.
        /// </summary>
        private async Task ForceSetAttachmentColumnsAsync(
           Guid id,
           long? size = null,
           AttachmentStatus? status = null,
           DateTimeOffset? completedAt = null,
           string? fileName = null,
           string? contentType = null)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var entity = await db.Attachments.FirstOrDefaultAsync(a => a.Id == id)
                         ?? throw new InvalidOperationException($"Attachment {id} not found.");

            var et = db.Model.FindEntityType(typeof(IncidentReportingSystem.Domain.Entities.Attachment))
                     ?? throw new InvalidOperationException("Entity type Attachment not found in EF model.");

            static IProperty? FindProp(IEntityType et, string[] preferred, Func<IProperty, bool>? typeFilter = null)
            {
                foreach (var name in preferred)
                {
                    var p = et.FindProperty(name);
                    if (p is null) continue;
                    if (typeFilter is null || typeFilter(p)) return p;
                }
                foreach (var p in et.GetProperties())
                {
                    if (typeFilter is not null && !typeFilter(p)) continue;
                    foreach (var needle in preferred)
                    {
                        if (p.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                            return p;
                    }
                }
                return null;
            }

            static bool IsNumeric(IProperty p)
                => p.ClrType == typeof(long) || p.ClrType == typeof(int) || p.ClrType == typeof(short)
                || p.ClrType == typeof(long?) || p.ClrType == typeof(int?) || p.ClrType == typeof(short?);

            static bool IsDateLike(IProperty p)
                => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTime)
                || p.ClrType == typeof(DateTimeOffset?) || p.ClrType == typeof(DateTime?);

            // ===== RESOLVE PROPERTIES =====
            var sizeProp = size.HasValue ? FindProp(et, new[] { "Size", "FileSize", "Length", "Bytes", "ContentLength" }, IsNumeric) : null;
            var statusProp = status.HasValue ? FindProp(et, new[] { "Status", "AttachmentStatus", "UploadStatus", "ProcessingStatus", "State" }) : null;
            var completedProp = completedAt.HasValue ? FindProp(et, new[] { "CompletedAt", "CompletedOn", "CompletedUtc", "CompletedAtUtc", "CompletionTime", "Completed" }, IsDateLike) : null;
            var fileNameProp = !string.IsNullOrWhiteSpace(fileName) ? FindProp(et, new[] { "FileName", "Filename", "OriginalFileName", "Name" }) : null;
            var contentTypeProp = !string.IsNullOrWhiteSpace(contentType) ? FindProp(et, new[] { "ContentType", "MimeType", "Mime", "MediaType" }) : null;

            var entry = db.Entry(entity);


            if (sizeProp is not null)
            {
                entry.Property(sizeProp.Name).CurrentValue = ConvertTo(sizeProp.ClrType, size!.Value);
            }

            if (statusProp is not null)
            {
                var t = statusProp.ClrType;
                object value;
                var enumType = Nullable.GetUnderlyingType(t) is { IsEnum: true } nt ? nt : (t.IsEnum ? t : null);
                if (enumType is not null)
                {
                    value = Enum.ToObject(enumType, (int)status!.Value);
                }
                else if (t == typeof(string))
                {
                    value = status!.Value.ToString();
                }
                else
                {
                    value = ConvertTo(t, (int)status!.Value);
                }

                entry.Property(statusProp.Name).CurrentValue = value;
            }

            if (completedProp is not null)
            {
                var t = completedProp.ClrType;
                var v = completedAt!.Value;
                object value = t == typeof(DateTime) || t == typeof(DateTime?)
                    ? v.UtcDateTime
                    : (object)v;
                entry.Property(completedProp.Name).CurrentValue = value;
            }

            if (fileNameProp is not null)
            {
                entry.Property(fileNameProp.Name).CurrentValue = fileName!;
            }

            if (contentTypeProp is not null)
            {
                entry.Property(contentTypeProp.Name).CurrentValue = contentType!;
            }

            await db.SaveChangesAsync();
        }
        private static object ConvertTo(Type target, object value)
        {
            if (value is null) return value!;
            var t = Nullable.GetUnderlyingType(target) ?? target;
            if (t.IsEnum) return Enum.ToObject(t, value);
            return Convert.ChangeType(value, t);
        }
    }
}
