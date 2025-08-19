using System;

namespace IncidentReportingSystem.Domain.Exceptions
{
    /// <summary>Represents a missing domain/application resource.</summary>
    public sealed class NotFoundException : Exception
    {
        public string Resource { get; }
        public string Key { get; }

        public NotFoundException(string resource, string key)
            : base($"{resource} '{key}' was not found.")
        {
            Resource = resource;
            Key = key;
        }
    }
}
