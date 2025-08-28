using System;
using System.Threading.Tasks;
using System.Linq;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class AttachmentRepository_FilteringTests
{
    private static ApplicationDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task ListByParentAsync_Filters_By_ParentType_And_ParentId_For_Comments()
    {
        using var db = NewDb();
        var repo = new AttachmentRepository(db);

        var incidentId = Guid.NewGuid();
        var commentA = Guid.NewGuid();
        var commentB = Guid.NewGuid();
        var uploader = Guid.NewGuid();

        // Seed 1 attachment for Incident, 2 for comments A/B
        var aIncident = new Attachment(AttachmentParentType.Incident, incidentId, "i.jpg", "image/jpeg", $"p/{incidentId}/i", uploader);
        aIncident.MarkCompleted(1, false);

        var aCommentA = new Attachment(AttachmentParentType.Comment, commentA, "ca.jpg", "image/jpeg", $"p/{commentA}/ca", uploader);
        aCommentA.MarkCompleted(1, false);

        var aCommentB = new Attachment(AttachmentParentType.Comment, commentB, "cb.jpg", "image/jpeg", $"p/{commentB}/cb", uploader);
        aCommentB.MarkCompleted(1, false);

        db.Attachments.AddRange(aIncident, aCommentA, aCommentB);
        await db.SaveChangesAsync();

        var (items, total) = await repo.ListByParentAsync(AttachmentParentType.Comment, commentA, 0, 100, default);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("ca.jpg", items.First().FileName);
        Assert.Equal(AttachmentParentType.Comment, items.First().ParentType);
        Assert.Equal(commentA, items.First().ParentId);
    }
}
