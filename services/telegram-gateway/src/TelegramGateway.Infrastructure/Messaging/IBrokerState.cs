using RabbitMQ.Client;

namespace TelegramGateway.Infrastructure.Messaging;

/// <summary>
/// Describes the RabbitMQ connection state used by gateway transports and health probes.
/// Example:
/// <code>
/// await state.Ensure(token);
/// IConnection link = await state.Connection(token);
/// </code>
/// </summary>
internal interface IBrokerState
{
    /// <summary>
    /// Gets the active broker connection.
    /// Example:
    /// <code>
    /// IConnection link = await state.Connection(token);
    /// </code>
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The active broker connection.</returns>
    public ValueTask<IConnection> Connection(CancellationToken token);
    /// <summary>
    /// Ensures the broker connection and topology are ready.
    /// Example:
    /// <code>
    /// await state.Ensure(token);
    /// </code>
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the transport is ready.</returns>
    public ValueTask Ensure(CancellationToken token);
}
