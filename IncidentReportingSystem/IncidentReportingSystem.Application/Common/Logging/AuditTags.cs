namespace IncidentReportingSystem.Application.Common.Logging;

/// <summary>
/// Canonical tag names used in logging scopes ("tags" field).
/// Keep tags short, stable, and composable (comma-separated).
/// </summary>
public static class AuditTags
{
    public const string Auth = "auth";
    public const string Register = "register";
    public const string Login = "login";

    public const string Attachments = "attachments";
    public const string Start = "start";
    public const string Complete = "complete";
    public const string Download = "download";

    public const string Incidents = "incidents";
    public const string Comments = "comments";
}
