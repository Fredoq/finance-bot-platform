using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Messaging;

/// <summary>
/// Maintains the long-lived RabbitMQ connection and exchange topology for the gateway.
/// Example:
/// <code>
/// await link.Ensure(token);
/// </code>
/// </summary>
internal sealed class RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log) : IBrokerState, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConnectionFactory factory = new()
    {
        Uri = new Uri(option.Value.Uri),
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        ClientProvidedName = option.Value.Client,
        RequestedHeartbeat = TimeSpan.FromSeconds(30)
    };
    private IConnection? link;
    /// <summary>
    /// Gets the active broker connection.
    /// Example:
    /// <code>
    /// IConnection item = await state.Connection(token);
    /// </code>
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The active broker connection.</returns>
    public async ValueTask<IConnection> Connection(CancellationToken token)
    {
        if (link is { IsOpen: true })
        {
            return link;
        }
        await gate.WaitAsync(token);
        try
        {
            if (link is { IsOpen: true })
            {
                return link;
            }
            link = await factory.CreateConnectionAsync(token);
            log.LogInformation("RabbitMQ connection is open");
            return link;
        }
        finally
        {
            gate.Release();
        }
    }
    /// <summary>
    /// Ensures the broker connection and topology are ready.
    /// Example:
    /// <code>
    /// await state.Ensure(token);
    /// </code>
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the transport is ready.</returns>
    public async ValueTask Ensure(CancellationToken token)
    {
        var item = await Connection(token);
        await using var lane = await item.CreateChannelAsync(cancellationToken: token);
        await lane.ExchangeDeclareAsync(option.Value.Exchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
    }
    /// <summary>
    /// Disposes the active broker connection.
    /// Example:
    /// <code>
    /// await link.DisposeAsync();
    /// </code>
    /// </summary>
    /// <returns>A task that completes when the connection is disposed.</returns>
    public async ValueTask DisposeAsync()
    {
        if (link is null)
        {
            return;
        }
        if (link.IsOpen)
        {
            await link.CloseAsync();
        }
        await link.DisposeAsync();
        gate.Dispose();
    }
}
