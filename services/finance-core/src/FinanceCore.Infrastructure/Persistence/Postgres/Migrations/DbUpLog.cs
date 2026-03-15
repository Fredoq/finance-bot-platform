using System.Globalization;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Migrations;

internal sealed class DbUpLog : IUpgradeLog
{
    private const string Template = "{Message}";
    private readonly ILogger<DbUpLog> log;
    internal DbUpLog(ILogger<DbUpLog> log) => this.log = log ?? throw new ArgumentNullException(nameof(log));
    public void LogTrace(string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);
        Write(LogLevel.Trace, format, args);
    }
    public void LogDebug(string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);
        Write(LogLevel.Debug, format, args);
    }
    public void LogInformation(string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);
        Write(LogLevel.Information, format, args);
    }
    public void LogWarning(string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);
        Write(LogLevel.Warning, format, args);
    }
    public void LogError(string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);
        Write(LogLevel.Error, format, args);
    }
    public void LogError(Exception ex, string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(args);
        Write(LogLevel.Error, ex, format, args);
    }
    private static string Text(string format, object[] args) => args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, format, args) : format;
    private void Write(LogLevel level, string format, object[] args)
    {
        if (!log.IsEnabled(level))
        {
            return;
        }
        string item = Text(format, args);
        log.Log(level, Template, item);
    }
    private void Write(LogLevel level, Exception? error, string format, object[] args)
    {
        if (!log.IsEnabled(level))
        {
            return;
        }
        string item = Text(format, args);
        if (error is null)
        {
            log.Log(level, Template, item);
            return;
        }
        log.Log(level, error, Template, item);
    }
}
