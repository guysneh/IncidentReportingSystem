using System.Linq;
using System.Threading.Tasks;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IncidentReportingSystem.Tests.Infrastructure.Persistence;

public sealed class AttachmentRepository_StatusFilterTests
{
    private static ApplicationDbContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opts);
    }

    [Fact]
    public async Task ListByParentAsync_Includes_InProgress_And_Completed_Items_And_Orders_NewestFirst()
    {
        using var db = NewDb();
        var repo = new AttachmentRepository(db);

        var parentId = Guid.NewGuid();
        var uploader = Guid.NewGuid();

        // Completed first
        var aCompleted = new Attachment(AttachmentParentType.Incident, parentId, "c1.bin", "application/octet-stream", $"p/{parentId}/c1", uploader);
        aCompleted.MarkCompleted(10, hasThumbnail: false);

        await Task.Delay(5); // ensure CreatedAt ordering

        // In-progress (not completed)
        var aInProgress = new Attachment(AttachmentParentType.Incident, parentId, "p1.bin", "application/octet-stream", $"p/{parentId}/p1", uploader);

        db.Attachments.AddRange(aCompleted, aInProgress);
        await db.SaveChangesAsync();

        var (items, total) = await repo.ListByParentAsync(AttachmentParentType.Incident, parentId, 0, 100, default);

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.Equal("p1.bin", items.First().FileName); // newest-first
        Assert.Equal("c1.bin", items.Last().FileName);
    }
}
