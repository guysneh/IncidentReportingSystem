using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.Tests.Infrastructure.Logging;

/// <summary>
/// In-memory ILoggerProvider that captures log entries and their active scopes,
/// so tests can assert EventId and custom scope fields (e.g., "tags").
/// </summary>
public sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<LogRecord> _records = new();
    private readonly AsyncLocal<Stack<object?>> _scope = new() { Value = new Stack<object?>() };

    public IReadOnlyCollection<LogRecord> Records => _records;

    public ILogger CreateLogger(string categoryName) => new TestLogger(this, categoryName);

    public void Dispose() { }

    private sealed class TestLogger : ILogger
    {
        private readonly TestLoggerProvider _p;
        private readonly string _category;

        public TestLogger(TestLoggerProvider provider, string category) =>
            (_p, _category) = (provider, category);

        public IDisposable BeginScope<TState>(TState state)
        {
            _p._scope.Value!.Push(state);
            return new Popper(_p._scope.Value!);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var snapshotScopes = _p._scope.Value!.Reverse().ToArray();
            var statePairs = state as IEnumerable<KeyValuePair<string, object>>;
            _p._records.Add(new LogRecord(
                _category, logLevel, eventId, statePairs?.ToArray() ?? Array.Empty<KeyValuePair<string, object>>(),
                snapshotScopes, exception, formatter(state, exception)));
        }

        private sealed class Popper : IDisposable
        {
            private readonly Stack<object?> _stack;
            private bool _disposed;
            public Popper(Stack<object?> stack) => _stack = stack;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (_stack.Count > 0) _stack.Pop();
            }
        }
    }

    public sealed record LogRecord(
        string Category,
        LogLevel Level,
        EventId EventId,
        IReadOnlyList<KeyValuePair<string, object>> State,
        IReadOnlyList<object?> Scopes,
        Exception? Exception,
        string Message)
    {
        public string? TryGetTags()
        {
            foreach (var s in Scopes)
            {
                if (s is IEnumerable<KeyValuePair<string, object>> kvs)
                {
                    var kv = kvs.FirstOrDefault(p => string.Equals(p.Key, "tags", StringComparison.Ordinal));
                    if (!kv.Equals(default(KeyValuePair<string, object>)))
                        return kv.Value?.ToString();
                }
            }
            return null;
        }
    }
}
