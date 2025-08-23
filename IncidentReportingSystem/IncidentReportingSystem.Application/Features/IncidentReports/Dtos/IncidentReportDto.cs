namespace IncidentReportingSystem.Application.Features.IncidentReports.Dtos;

/// <summary>
/// Data Transfer Object for returning incident reports via API.
/// </summary>
public class IncidentReportDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = default!;
    public string Location { get; set; } = default!;
    public Guid ReporterId { get; set; }
    public string Category { get; set; } = default!;
    public string SystemAffected { get; set; } = default!;
    public string Severity { get; set; } = default!;
    public DateTime? ReportedAt { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
