using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

/// <summary>
/// Provides a ready broker state for API tests that do not need a real RabbitMQ connection.
/// Example:
/// <code>
/// var state = new ReadyBrokerState();
/// </code>
/// </summary>
internal sealed class ReadyBrokerState : IBrokerState
{
    /// <summary>
    /// Throws because this fake never exposes a real broker connection.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <exception cref="NotSupportedException">Thrown whenever a test asks this fake for an IConnection.</exception>
    public ValueTask<IConnection> Connection(CancellationToken token) => throw new NotSupportedException("Connection is not used by this fake");
    /// <summary>
    /// Marks the broker state as ready.
    /// Example:
    /// <code>
    /// await state.Ensure(token);
    /// </code>
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public ValueTask Ensure(CancellationToken token) => ValueTask.CompletedTask;
}
