using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Mappers;

/// <summary>
/// Provides mapping logic from IncidentReport domain entity to DTO.
/// </summary>
public static class IncidentReportMapper
{
    /// <summary>
    /// Maps a domain IncidentReport entity to a DTO used for API responses.
    /// </summary>
    public static IncidentReportDto ToDto(this IncidentReport entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        return new IncidentReportDto
        {
            Id = entity.Id,
            Description = entity.Description,
            Location = entity.Location,
            ReporterId = entity.ReporterId,
            Category = entity.Category.ToString(),
            SystemAffected = entity.SystemAffected,
            Severity = entity.Severity.ToString(),
            ReportedAt = entity.ReportedAt,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt
        };
    }
}
