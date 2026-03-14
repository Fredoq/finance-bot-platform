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
        HostName = option.Value.Host,
        Port = option.Value.Port,
        VirtualHost = option.Value.VirtualHost,
        UserName = option.Value.Username,
        Password = option.Value.Password,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        ClientProvidedName = option.Value.Client,
        RequestedHeartbeat = TimeSpan.FromSeconds(30)
    };
    private IConnection? link;
    private int disposed;
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
        if (Volatile.Read(ref disposed) == 1)
        {
            throw new ObjectDisposedException(nameof(RabbitMqLink));
        }
        if (link is { IsOpen: true })
        {
            return link;
        }
        await gate.WaitAsync(token);
        try
        {
            if (Volatile.Read(ref disposed) == 1)
            {
                throw new ObjectDisposedException(nameof(RabbitMqLink));
            }
            if (link is { IsOpen: true })
            {
                return link;
            }
            if (link is not null)
            {
                await Close(link);
                link = null;
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
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(cancellationToken: token);
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
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }
        await gate.WaitAsync();
        try
        {
            if (link is not null)
            {
                await Close(link);
                link = null;
            }
        }
        finally
        {
            gate.Release();
            gate.Dispose();
        }
    }
    /// <summary>
    /// Closes and disposes a stale broker connection.
    /// Example:
    /// <code>
    /// await link.Close(item);
    /// </code>
    /// </summary>
    /// <param name="item">The connection to close.</param>
    /// <returns>A task that completes when the connection is released.</returns>
    private async ValueTask Close(IConnection item)
    {
        try
        {
            if (item.IsOpen)
            {
                await item.CloseAsync();
            }
            await item.DisposeAsync();
        }
        catch (Exception error)
        {
            log.LogWarning(error, "RabbitMQ connection cleanup failed");
        }
    }
}
