using Microsoft.Extensions.Logging;
using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Flow;

/// <summary>
/// Selects one specialized Telegram slice for the inbound update and runs it.
/// Example:
/// <code>
/// await flow.Run(update, "trace-1", token);
/// </code>
/// </summary>
internal sealed class TelegramFlow(IEnumerable<ITelegramSlice> flow, ILogger<TelegramFlow> log) : ITelegramFlow
{
    private readonly ITelegramSlice[] list = [.. flow];
    /// <summary>
    /// Runs the single matching slice for the inbound update.
    /// Example:
    /// <code>
    /// await flow.Run(update, "trace-1", token);
    /// </code>
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <param name="trace">The correlation value for the current request.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when processing finishes.</returns>
    public async ValueTask Run(TelegramUpdate update, string trace, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(update);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace);
        ArgumentNullException.ThrowIfNull(log);
        ITelegramSlice[] item = [.. list.Where(item => item.Match(update)).Take(2)];
        if (item.Length == 0)
        {
            log.LogInformation("Telegram update was ignored because no flow matched");
            return;
        }
        if (item.Length > 1)
        {
            throw new InvalidOperationException("Telegram update matched multiple flows");
        }
        await item[0].Run(update, trace, token);
    }
}
