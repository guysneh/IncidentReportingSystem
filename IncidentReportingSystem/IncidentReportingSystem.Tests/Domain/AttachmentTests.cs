using System;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Xunit;

namespace IncidentReportingSystem.Tests.Domain;

public class AttachmentTests
{
    [Fact]
    public void New_Attachment_Defaults()
    {
        var now = DateTime.UtcNow;
        var a = new Attachment
        (
            parentType: AttachmentParentType.Incident,
            parentId: Guid.NewGuid(),
            fileName: "a.png",
            contentType: "image/png",
            initialStoragePath: "incidents/xxx/yyy/a.png",
            uploadedBy: Guid.NewGuid()
        );

        Assert.Equal(AttachmentStatus.Pending, a.Status);
        Assert.Null(a.CompletedAt);
        Assert.False(a.HasThumbnail);
    }

    [Fact]
    public void MarkCompleted_SetsStatusAndCompletedAt()
    {
        var a = new Attachment
        (
            parentType: AttachmentParentType.Incident,
            parentId: Guid.NewGuid(),
            fileName: "a.png",
            contentType: "image/png",
            initialStoragePath: "incidents/xxx/yyy/a.png",
            uploadedBy: Guid.NewGuid()
        );

        Assert.Null(a.CompletedAt);
        Assert.NotEqual(AttachmentStatus.Completed, a.Status);

        a.MarkCompleted(long.MinValue);

        Assert.Equal(AttachmentStatus.Completed, a.Status);
        Assert.NotNull(a.CompletedAt);
        Assert.True(a.CompletedAt!.Value <= DateTime.UtcNow.AddSeconds(1));
    }
}
