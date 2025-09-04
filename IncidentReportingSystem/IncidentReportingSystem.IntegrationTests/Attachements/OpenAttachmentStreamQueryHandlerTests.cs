using System.Text;
using FluentAssertions;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Moq;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.Features.Attachments;

public sealed class OpenAttachmentStreamQueryHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Stream_And_ETag_For_Completed_Attachment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new Attachment(
            AttachmentParentType.Incident, Guid.NewGuid(),
            "file.png", "image/png", "incidents/x/y/file.png", Guid.NewGuid());
        a.MarkCompleted(12);

        var repo = new Mock<IAttachmentRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetReadOnlyAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(a);

        var storage = new Mock<IAttachmentStorage>(MockBehavior.Strict);
        storage.Setup(s => s.TryGetUploadedAsync(a.StoragePath, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new UploadedBlobProps(12, "image/png", "\"etag-123\""));
        storage.Setup(s => s.OpenReadAsync(a.StoragePath, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("hello")));

        var sut = new OpenAttachmentStreamQueryHandler(repo.Object, storage.Object);

        // Act
        var resp = await sut.Handle(new OpenAttachmentStreamQuery(id), CancellationToken.None);

        // Assert
        resp.ContentType.Should().Be("image/png");
        resp.FileName.Should().Be("file.png");
        resp.ETag.Should().Be("\"etag-123\"");
        using var ms = new MemoryStream();
        await resp.Stream.CopyToAsync(ms);
        Encoding.UTF8.GetString(ms.ToArray()).Should().Be("hello");

        repo.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task Handle_Throws_When_Not_Completed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new Attachment(
            AttachmentParentType.Comment, Guid.NewGuid(),
            "file.pdf", "application/pdf", "comments/x/y/file.pdf", Guid.NewGuid());
        // Note: not calling MarkCompleted → Status = Pending

        var repo = new Mock<IAttachmentRepository>();
        repo.Setup(r => r.GetReadOnlyAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(a);

        var storage = new Mock<IAttachmentStorage>(MockBehavior.Loose);

        var sut = new OpenAttachmentStreamQueryHandler(repo.Object, storage.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(new OpenAttachmentStreamQuery(id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_When_Storage_Object_Missing()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new Attachment(
            AttachmentParentType.Incident, Guid.NewGuid(),
            "x.jpg", "image/jpeg", "incidents/x/y/x.jpg", Guid.NewGuid());
        a.MarkCompleted(7);

        var repo = new Mock<IAttachmentRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetReadOnlyAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(a);

        var storage = new Mock<IAttachmentStorage>(MockBehavior.Strict);
        storage.Setup(s => s.TryGetUploadedAsync(a.StoragePath, It.IsAny<CancellationToken>()))
               .ReturnsAsync((UploadedBlobProps?)null);

        var sut = new OpenAttachmentStreamQueryHandler(repo.Object, storage.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Handle(new OpenAttachmentStreamQuery(id), CancellationToken.None));

        repo.VerifyAll();
        storage.VerifyAll();
    }
}
