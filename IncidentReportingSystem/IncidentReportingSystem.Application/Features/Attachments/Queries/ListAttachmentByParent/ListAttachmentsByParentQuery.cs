using System;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.Domain.Enums;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent
{
    /// <summary>
    /// Lists attachments for a given parent (Incident/Comment) with search/filter/sort/paging support.
    /// </summary>
    public sealed record ListAttachmentsByParentQuery(
        AttachmentParentType ParentType,
        Guid ParentId,
        AttachmentListFilters Filters
    ) : IRequest<PagedResult<AttachmentDto>>;
}
