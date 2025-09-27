namespace IncidentReportingSystem.Application.Common.Identity;

public static class DisplayNameFormatter
{
    /// <summary>
    /// Builds a display name: "First Last" if both exist; otherwise email; otherwise empty string.
    /// Never returns the Id as a display name.
    /// </summary>
    public static string Build(string? first, string? last, string? email)
    {
        if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
            return $"{first} {last}";
        if (!string.IsNullOrWhiteSpace(email))
            return email!;
        return string.Empty;
    }
}
