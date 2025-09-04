// namespace IncidentReportingSystem.IntegrationTests.Infrastructure.Logging (or your current one)
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogRecord> _records = new();
    public IReadOnlyList<LogRecord> Records => _records.ToArray();

    public IReadOnlyList<LogRecord> Snapshot() => _records.ToArray();

    public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _records);

    public void Dispose() { }

    private sealed class TestLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<LogRecord> _sink;

        public TestLogger(string category, ConcurrentQueue<LogRecord> sink)
        {
            _category = category;
            _sink = sink;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Copy state to a plain array to avoid future mutation/races
            var kvs = state as IEnumerable<KeyValuePair<string, object?>> ?? Array.Empty<KeyValuePair<string, object?>>();
            var stateCopy = kvs.Select(kv => new KeyValuePair<string, object?>(kv.Key, kv.Value)).ToArray();

            _sink.Enqueue(new LogRecord(
                Level: logLevel,
                EventId: eventId,
                Category: _category,
                Message: formatter(state, exception),
                State: stateCopy,
                Exception: exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

// Plain record your tests already use
public sealed record LogRecord(
    LogLevel Level,
    EventId EventId,
    string Category,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> State,
    Exception? Exception);

public static class LogRecordExtensions
{
    public static string? TryGetTags(this LogRecord r)
    {
        var tagKv = r.State.FirstOrDefault(kv => string.Equals(kv.Key, "tags", StringComparison.OrdinalIgnoreCase));
        return tagKv.Value?.ToString();
    }
}
