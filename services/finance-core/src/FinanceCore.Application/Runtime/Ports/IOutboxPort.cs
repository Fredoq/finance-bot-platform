using Finance.Application.Contracts.Messaging;

namespace FinanceCore.Application.Runtime.Ports;

/// <summary>
/// Persists outbound envelopes for reliable delivery.
/// </summary>
public interface IOutboxPort
{
    /// <summary>
    /// Saves an outbound envelope.
    /// </summary>
    /// <param name="message">The outbound envelope.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when persistence finishes.</returns>
    ValueTask Save<TMessage>(MessageEnvelope<TMessage> message, string routingKey, CancellationToken token) where TMessage : class;
}
