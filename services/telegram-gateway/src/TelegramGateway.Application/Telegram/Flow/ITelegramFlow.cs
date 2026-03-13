using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Flow;

/// <summary>
/// Describes the Telegram webhook flow that processes one inbound update.
/// Example:
/// <code>
/// await flow.Run(update, "trace-1", token);
/// </code>
/// </summary>
public interface ITelegramFlow
{
    /// <summary>
    /// Processes the inbound update and publishes one downstream message when a matching slice exists.
    /// Example:
    /// <code>
    /// await flow.Run(update, "trace-1", token);
    /// </code>
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <param name="trace">The correlation value for the current request.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when processing finishes.</returns>
    public ValueTask Run(TelegramUpdate update, string trace, CancellationToken token);
}
