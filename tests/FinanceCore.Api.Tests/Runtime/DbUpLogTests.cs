using DbUp.Engine.Output;
using FinanceCore.Infrastructure.Persistence.Postgres.Migrations;
using Microsoft.Extensions.Logging;

namespace FinanceCore.Api.Tests;

/// <summary>
/// Covers DbUp log forwarding behavior.
/// </summary>
public sealed class DbUpLogTests
{
    /// <summary>
    /// Verifies that every DbUp log level is forwarded through the fixed template.
    /// </summary>
    /// <returns>A task that completes when the assertions finish.</returns>
    [Fact(DisplayName = "Forwards DbUp messages through the fixed logger template")]
    public Task Forwards_messages()
    {
        var sink = new LogSink<DbUpLog>();
        DbUpLog item = new(sink);
        item.LogTrace("trace {0}", 1);
        item.LogDebug("debug {0}", 2);
        item.LogInformation("info {0}", 3);
        item.LogWarning("warning {0}", 4);
        item.LogError("error {0}", 5);
        item.LogError(new InvalidOperationException("broken"), "fatal {0}", 6);
        Assert.Equal(6, sink.Items.Count);
        Assert.All(sink.Items, note => Assert.Equal("{Message}", note.Template));
        return Task.CompletedTask;
    }
    private sealed record LogItem(LogLevel Level, string Template, Exception? Error, object?[] Args);
    private sealed class LogSink<T> : ILogger<T>
    {
        public List<LogItem> Items { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IReadOnlyList<KeyValuePair<string, object?>> list = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            string template = list.FirstOrDefault(pair => pair.Key == "{OriginalFormat}").Value?.ToString() ?? string.Empty;
            object?[] args = list.Where(pair => pair.Key != "{OriginalFormat}").Select(pair => pair.Value).ToArray();
            Items.Add(new LogItem(logLevel, template, exception, args));
        }
    }
}
