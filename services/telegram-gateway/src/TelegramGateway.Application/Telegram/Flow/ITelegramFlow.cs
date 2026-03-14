using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Flow;

/// <summary>
/// Describes the Telegram webhook flow that processes one inbound update.
/// </summary>
public interface ITelegramFlow
{
    /// <summary>
    /// Processes one inbound Telegram update.
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <param name="trace">The trace identifier.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    ValueTask Run(TelegramUpdate update, string trace, CancellationToken token);
}
