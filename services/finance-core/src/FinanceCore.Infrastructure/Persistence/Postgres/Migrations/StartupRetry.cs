using Microsoft.Extensions.Logging;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Migrations;

internal static class StartupRetry
{
    internal static async Task Run(string error, Func<CancellationToken, ValueTask> probe, ILogger log, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(log);
        const int Count = 5;
        var span = TimeSpan.FromSeconds(1);
        for (int item = 1; item <= Count; item++)
        {
            try
            {
                await probe(token);
                return;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception note) when (item < Count)
            {
                if (log.IsEnabled(LogLevel.Warning))
                {
                    log.LogWarning(note, error, item, Count);
                }
                await Task.Delay(span, token);
                span = TimeSpan.FromSeconds(Math.Min(span.TotalSeconds * 2, 15));
            }
        }
    }
}
