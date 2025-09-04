using System;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent
{
    /// <summary>
    /// Search, filter, sort and paging parameters for listing attachments.
    /// All values are optional. Defaults are applied in the handler.
    /// </summary>
    public sealed record AttachmentListFilters(
        string? Search = null,                  // q: substring on FileName (case-insensitive)
        string? ContentType = null,             // exact match
        DateTimeOffset? CreatedAfter = null,    // inclusive
        DateTimeOffset? CreatedBefore = null,   // inclusive
        string OrderBy = "createdAt",           // createdAt | fileName | size
        string Direction = "desc",              // asc | desc
        int Skip = 0,
        int Take = 100
    );
}
