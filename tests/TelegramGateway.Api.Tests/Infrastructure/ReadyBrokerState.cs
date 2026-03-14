using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Messaging;

namespace TelegramGateway.Api.Tests.Infrastructure;

internal sealed class ReadyBrokerState : IBrokerState
{
    /// <summary>
    /// Rejects direct connection access for the readiness fake.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The broker connection.</returns>
    public ValueTask<IConnection> Connection(CancellationToken token) => throw new NotSupportedException("Connection is not used by this fake");
    /// <summary>
    /// Returns a ready state for the fake broker.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns><see langword="true"/>.</returns>
    public ValueTask<bool> Ready(CancellationToken token) => ValueTask.FromResult(true);
    /// <summary>
    /// Completes immediately for the fake broker.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    public ValueTask Ensure(CancellationToken token) => ValueTask.CompletedTask;
}
