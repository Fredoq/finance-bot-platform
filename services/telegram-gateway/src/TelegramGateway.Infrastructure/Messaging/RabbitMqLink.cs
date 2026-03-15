using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqLink : IBrokerState, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConnectionFactory factory;
    private readonly RabbitMqOptions option;
    private readonly ILogger<RabbitMqLink> log;
    private IConnection? link;
    private int disposed;
    public RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log)
    {
        ArgumentNullException.ThrowIfNull(option);
        this.option = option.Value;
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        factory = new ConnectionFactory
        {
            HostName = this.option.Host,
            Port = this.option.Port,
            VirtualHost = this.option.VirtualHost,
            UserName = this.option.Username,
            Password = this.option.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = this.option.Client,
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        };
    }
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
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        await lane.ExchangeDeclareAsync(option.CommandExchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(option.DeliveryExchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(Retry(), ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(Resume(), ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(Dead(), ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        _ = await lane.QueueDeclareAsync(option.DeliveryQueue, true, false, false, new Dictionary<string, object?> { ["x-dead-letter-exchange"] = Retry(), ["x-dead-letter-routing-key"] = option.DeliveryQueue }, false, token);
        _ = await lane.QueueDeclareAsync(option.DeliveryRetryQueue, true, false, false, new Dictionary<string, object?> { ["x-message-ttl"] = option.DeliveryRetryDelaySeconds * 1000, ["x-dead-letter-exchange"] = Resume(), ["x-dead-letter-routing-key"] = option.DeliveryQueue }, false, token);
        _ = await lane.QueueDeclareAsync(option.DeliveryDeadQueue, true, false, false, arguments: null, noWait: false, cancellationToken: token);
        await lane.QueueBindAsync(option.DeliveryQueue, option.DeliveryExchange, "#", null, false, token);
        await lane.QueueBindAsync(option.DeliveryQueue, Resume(), option.DeliveryQueue, null, false, token);
        await lane.QueueBindAsync(option.DeliveryRetryQueue, Retry(), option.DeliveryQueue, null, false, token);
        await lane.QueueBindAsync(option.DeliveryDeadQueue, Dead(), option.DeliveryQueue, null, false, token);
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
    private string Retry() => $"{option.DeliveryExchange}.retry";
    private string Resume() => $"{option.DeliveryExchange}.resume";
    private string Dead() => $"{option.DeliveryExchange}.dead";
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
