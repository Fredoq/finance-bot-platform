using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Flow;

/// <summary>
/// Describes one specialized Telegram slice that can match and process an inbound update.
/// Example:
/// <code>
/// bool item = slice.Match(update);
/// await slice.Run(update, "trace-1", token);
/// </code>
/// </summary>
internal interface ITelegramSlice
{
    /// <summary>
    /// Indicates whether the slice supports the inbound update.
    /// Example:
    /// <code>
    /// bool item = slice.Match(update);
    /// </code>
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <returns><see langword="true"/> when the slice supports the update.</returns>
    public bool Match(TelegramUpdate update);
    /// <summary>
    /// Processes the inbound update after a successful match.
    /// Example:
    /// <code>
    /// await slice.Run(update, "trace-1", token);
    /// </code>
    /// </summary>
    /// <param name="update">The inbound update.</param>
    /// <param name="trace">The correlation value for the current request.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the slice finishes.</returns>
    public ValueTask Run(TelegramUpdate update, string trace, CancellationToken token);
}
