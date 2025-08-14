
namespace IncidentReportingSystem.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Sets a group of environment variables for the current process and restores
    /// previous values (or clears them) when disposed.
    /// </summary>
    public sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originals = new();

        public EnvironmentVariableScope(IDictionary<string, string> variables)
        {
            foreach (var kv in variables)
            {
                _originals[kv.Key] = Environment.GetEnvironmentVariable(kv.Key);
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
            }
        }

        public void Dispose()
        {
            foreach (var kv in _originals)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
            }
        }
    }
}
