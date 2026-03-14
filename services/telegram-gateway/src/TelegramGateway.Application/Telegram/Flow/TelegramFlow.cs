using Microsoft.Extensions.Logging;
using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Flow;

internal sealed class TelegramFlow(IEnumerable<ITelegramSlice> flow, ILogger<TelegramFlow> log) : ITelegramFlow
{
    private readonly ITelegramSlice[] list = [.. flow];
    /// <summary>
    /// Processes one inbound Telegram update through the matching slice.
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <param name="trace">The trace identifier.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
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
