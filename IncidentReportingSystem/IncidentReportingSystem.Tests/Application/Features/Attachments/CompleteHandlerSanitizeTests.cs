using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Logging;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.Attachments;
using IncidentReportingSystem.Application.Features.Attachments.Commands;
using IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Attachments;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.Tests.Attachments
{
    public sealed class CompleteHandlerSanitizeTests
    {
        [Fact]
        public async Task When_Sanitize_Enabled_And_Image_File_Size_Is_Updated_To_Sanitized_Length()
        {
            var att = new Attachment(AttachmentParentType.Incident, Guid.NewGuid(), "a.jpg", "image/jpeg", "incidents/x/a.jpg", Guid.NewGuid());
            var repo = new Mock<IAttachmentRepository>();
            repo.Setup(r => r.GetAsync(att.Id, It.IsAny<CancellationToken>())).ReturnsAsync(att);

            var policy = new Mock<IAttachmentPolicy>();
            policy.SetupGet(p => p.MaxSizeBytes).Returns(10_000_000);

            var storage = new Mock<IAttachmentStorage>();
            storage.Setup(s => s.TryGetUploadedAsync(att.StoragePath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new UploadedBlobProps(1234, "image/jpeg", "\"etag\""));

            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var audit = new Mock<IAttachmentAuditService>();
            var opts = Options.Create(new AttachmentOptions { SanitizeImages = true });

            var sanitizer = new Mock<IImageSanitizer>();
            sanitizer.Setup(s => s.TrySanitizeAsync(att.StoragePath, "image/jpeg", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((true, 1111L, "image/jpeg"));

            var handler = new CompleteUploadAttachmentCommandHandler(
                repo.Object, policy.Object, storage.Object, uow.Object, audit.Object, opts, sanitizer.Object);

            await handler.Handle(new CompleteUploadAttachmentCommand(att.Id), CancellationToken.None);

            Assert.Equal(AttachmentStatus.Completed, att.Status);
            Assert.Equal(1111L, att.Size); // updated to sanitized length
            audit.Verify(a => a.AttachmentCompleted(att.Id), Times.Once);
        }

        [Fact]
        public async Task When_Sanitize_Disabled_Size_Remains_From_StorageProps()
        {
            var att = new Attachment(AttachmentParentType.Incident, Guid.NewGuid(), "a.png", "image/png", "incidents/x/a.png", Guid.NewGuid());
            var repo = new Mock<IAttachmentRepository>();
            repo.Setup(r => r.GetAsync(att.Id, It.IsAny<CancellationToken>())).ReturnsAsync(att);

            var policy = new Mock<IAttachmentPolicy>();
            policy.SetupGet(p => p.MaxSizeBytes).Returns(10_000_000);

            var storage = new Mock<IAttachmentStorage>();
            storage.Setup(s => s.TryGetUploadedAsync(att.StoragePath, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new UploadedBlobProps(2222, "image/png", "\"etag\""));

            var uow = new Mock<IUnitOfWork>();
            var audit = new Mock<IAttachmentAuditService>();
            var opts = Options.Create(new AttachmentOptions { SanitizeImages = false });

            var sanitizer = new Mock<IImageSanitizer>();

            var handler = new CompleteUploadAttachmentCommandHandler(
                repo.Object, policy.Object, storage.Object, uow.Object, audit.Object, opts, sanitizer.Object);

            await handler.Handle(new CompleteUploadAttachmentCommand(att.Id), CancellationToken.None);

            Assert.Equal(AttachmentStatus.Completed, att.Status);
            Assert.Equal(2222L, att.Size); // unchanged
            sanitizer.Verify(s => s.TrySanitizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
