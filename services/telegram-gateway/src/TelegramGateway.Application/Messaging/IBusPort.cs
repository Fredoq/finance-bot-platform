using Finance.Application.Contracts.Messaging;

namespace TelegramGateway.Application.Messaging;

/// <summary>
/// Describes the outbound port that publishes application messages.
/// </summary>
public interface IBusPort
{
    /// <summary>
    /// Publishes an application message envelope.
    /// </summary>
    /// <param name="message">The message envelope.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the publish finishes.</returns>
    ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class;
}
