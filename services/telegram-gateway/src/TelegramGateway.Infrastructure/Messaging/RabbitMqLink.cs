using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Messaging;

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
    /// Opens or returns the active RabbitMQ connection.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The active broker connection.</returns>
    public async ValueTask<IConnection> Connection(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, nameof(RabbitMqLink));
        if (link is { IsOpen: true })
        {
            return link;
        }
        await gate.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, nameof(RabbitMqLink));
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
    /// Checks whether the broker connection is ready.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns><see langword="true"/> when the broker is ready; otherwise <see langword="false"/>.</returns>
    public async ValueTask<bool> Ready(CancellationToken token)
    {
        IConnection item = await Connection(token);
        return item.IsOpen;
    }
    /// <summary>
    /// Ensures that the broker exchange exists.
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the broker is ready.</returns>
    public async ValueTask Ensure(CancellationToken token)
    {
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(cancellationToken: token);
        await lane.ExchangeDeclareAsync(option.Value.Exchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
    }
    /// <summary>
    /// Disposes the broker connection resources.
    /// </summary>
    /// <returns>A task that completes when disposal finishes.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            link = null;
            log.LogWarning("RabbitMQ connection disposal timed out");
            return;
        }
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
