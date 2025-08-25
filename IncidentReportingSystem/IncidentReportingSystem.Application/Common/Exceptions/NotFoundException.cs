namespace IncidentReportingSystem.Application.Common.Exceptions
{
    /// <summary>Represents a missing domain/application resource.</summary>
    public sealed class NotFoundException : Exception
    {
        public string Resource { get; } = String.Empty;
        public string Key { get; } = String.Empty;

        public NotFoundException(string resource, string key)
            : base($"{resource} '{key}' was not found.")
        {
            Resource = resource;
            Key = key;
        }
        public NotFoundException(string message) : base(message) { }
    }
}
