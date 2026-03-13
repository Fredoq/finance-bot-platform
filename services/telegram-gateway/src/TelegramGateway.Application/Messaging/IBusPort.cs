using Finance.Application.Contracts.Messaging;

namespace TelegramGateway.Application.Messaging;

/// <summary>
/// Describes the outbound port that publishes application messages.
/// Example:
/// <code>
/// await port.Publish(message, token);
/// </code>
/// </summary>
public interface IBusPort
{
    /// <summary>
    /// Publishes the application message envelope to the async boundary.
    /// Example:
    /// <code>
    /// await port.Publish(message, token);
    /// </code>
    /// </summary>
    /// <param name="message">The envelope to publish.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the broker confirms the publish.</returns>
    public ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class;
}
