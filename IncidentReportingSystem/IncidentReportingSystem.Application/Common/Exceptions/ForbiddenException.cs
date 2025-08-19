namespace IncidentReportingSystem.Application.Common.Exceptions
{
    /// <summary>Represents an authorization failure (authenticated but not allowed).</summary>
    public sealed class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
