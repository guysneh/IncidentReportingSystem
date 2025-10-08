using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using IncidentReportingSystem.Domain.Entities;

public static class IncidentReportMappers
{
    public static IncidentReportDto ToDto(this IncidentReport e)
        => new IncidentReportDto
        {
            Id = e.Id,
            Description = e.Description,
            Location = e.Location,
            ReporterId = e.ReporterId,
            Category = e.Category.ToString(),
            SystemAffected = e.SystemAffected,
            Severity = e.Severity.ToString(),
            ReportedAt = e.ReportedAt,
            Status = e.Status.ToString(),
            CreatedAt = e.CreatedAt,
            ModifiedAt = e.ModifiedAt
        };

    public static IncidentReportDto ToDto(this IncidentReport e, string? reporterDisplayName)
    {
        var dto = e.ToDto();
        dto.ReporterDisplayName = reporterDisplayName;
        return dto;
    }
}
