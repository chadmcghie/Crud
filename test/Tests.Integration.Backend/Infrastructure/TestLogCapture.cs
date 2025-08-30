using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Helper class to capture and expose logs during integration tests
/// Useful for debugging server-side errors that result in HTTP 500 responses
/// </summary>
public class TestLogCapture : ILogger
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private readonly string _categoryName;

    public TestLogCapture(string categoryName = "TestLogger")
    {
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => NullDisposable.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var entry = new LogEntry
        {
            LogLevel = logLevel,
            CategoryName = _categoryName,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception,
            Timestamp = DateTime.UtcNow
        };

        _logs.Enqueue(entry);

        // Also output to console for immediate visibility during test runs
        if (logLevel >= LogLevel.Warning)
        {
            var prefix = logLevel switch
            {
                LogLevel.Critical => "🔥 CRITICAL",
                LogLevel.Error => "❌ ERROR",
                LogLevel.Warning => "⚠️  WARNING",
                LogLevel.Information => "ℹ️  INFO",
                LogLevel.Debug => "🐛 DEBUG",
                LogLevel.Trace => "📝 TRACE",
                _ => logLevel.ToString().ToUpper()
            };

            Console.WriteLine($"{prefix} [{_categoryName}]: {entry.Message}");
            if (exception != null)
            {
                Console.WriteLine($"   Exception: {exception}");
            }
        }
    }

    /// <summary>
    /// Gets all captured logs
    /// </summary>
    public IReadOnlyList<LogEntry> GetLogs() => _logs.ToList();

    /// <summary>
    /// Gets logs filtered by minimum log level
    /// </summary>
    public IReadOnlyList<LogEntry> GetLogs(LogLevel minimumLevel) => 
        _logs.Where(l => l.LogLevel >= minimumLevel).ToList();

    /// <summary>
    /// Gets error and critical logs
    /// </summary>
    public IReadOnlyList<LogEntry> GetErrorLogs() => 
        _logs.Where(l => l.LogLevel >= LogLevel.Error).ToList();

    /// <summary>
    /// Outputs all captured logs to console for debugging
    /// </summary>
    public void DumpLogsToConsole(LogLevel minimumLevel = LogLevel.Information)
    {
        var logs = GetLogs(minimumLevel);
        if (logs.Any())
        {
            Console.WriteLine($"\n📋 Captured Logs ({logs.Count} entries):");
            Console.WriteLine(new string('=', 80));

            foreach (var log in logs)
            {
                var prefix = log.LogLevel switch
                {
                    LogLevel.Critical => "🔥 CRITICAL",
                    LogLevel.Error => "❌ ERROR",
                    LogLevel.Warning => "⚠️  WARNING",
                    LogLevel.Information => "ℹ️  INFO",
                    LogLevel.Debug => "🐛 DEBUG",
                    LogLevel.Trace => "📝 TRACE",
                    _ => log.LogLevel.ToString().ToUpper()
                };

                Console.WriteLine($"{log.Timestamp:HH:mm:ss.fff} {prefix} [{log.CategoryName}]: {log.Message}");
                if (log.Exception != null)
                {
                    Console.WriteLine($"   Exception: {log.Exception}");
                }
            }
            Console.WriteLine(new string('=', 80));
        }
        else
        {
            Console.WriteLine($"\n📋 No logs captured at {minimumLevel} level or higher");
        }
    }

    /// <summary>
    /// Clears all captured logs
    /// </summary>
    public void Clear()
    {
        while (_logs.TryDequeue(out _)) { }
    }

    /// <summary>
    /// Checks if there are any error or critical logs
    /// </summary>
    public bool HasErrors => _logs.Any(l => l.LogLevel >= LogLevel.Error);

    /// <summary>
    /// Gets the most recent error log entry, if any
    /// </summary>
    public LogEntry? GetLatestError() => 
        _logs.Where(l => l.LogLevel >= LogLevel.Error).OrderByDescending(l => l.Timestamp).FirstOrDefault();
}

/// <summary>
/// Represents a captured log entry
/// </summary>
public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Logger provider for TestLogCapture
/// </summary>
public class TestLogCaptureProvider : ILoggerProvider
{
    private readonly TestLogCapture _logCapture;

    public TestLogCaptureProvider(TestLogCapture logCapture)
    {
        _logCapture = logCapture;
    }

    public ILogger CreateLogger(string categoryName) => _logCapture;

    public void Dispose() { }
}

/// <summary>
/// Null disposable implementation
/// </summary>
internal class NullDisposable : IDisposable
{
    public static readonly NullDisposable Instance = new();
    public void Dispose() { }
}