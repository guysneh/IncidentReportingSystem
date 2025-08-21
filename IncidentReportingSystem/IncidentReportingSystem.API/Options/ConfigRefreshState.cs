namespace IncidentReportingSystem.API.Options;

public sealed class ConfigRefreshState
{
    public DateTimeOffset LastRefreshUtc { get; set; } = DateTimeOffset.MinValue;
}
