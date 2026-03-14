using System.Globalization;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Migrations;

internal sealed class DbUpLog : IUpgradeLog
{
    private const string Template = "{Message}";
    private readonly ILogger<DbUpLog> log;
    internal DbUpLog(ILogger<DbUpLog> log) => this.log = log ?? throw new ArgumentNullException(nameof(log));
    public void LogTrace(string format, params object[] args) => log.LogTrace(Template, Text(format, args));
    public void LogDebug(string format, params object[] args) => log.LogDebug(Template, Text(format, args));
    public void LogInformation(string format, params object[] args) => log.LogInformation(Template, Text(format, args));
    public void LogWarning(string format, params object[] args) => log.LogWarning(Template, Text(format, args));
    public void LogError(string format, params object[] args) => log.LogError(Template, Text(format, args));
    public void LogError(Exception ex, string format, params object[] args) => log.LogError(ex, Template, Text(format, args));
    private static string Text(string format, object[] args) => args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, format, args) : format;
}
