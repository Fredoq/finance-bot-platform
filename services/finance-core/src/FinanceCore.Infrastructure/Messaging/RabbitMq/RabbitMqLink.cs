using FinanceCore.Infrastructure.Configuration.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqLink : IBrokerState, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConnectionFactory factory;
    private readonly RabbitMqOptions option;
    private readonly ILogger<RabbitMqLink> log;
    private IConnection? link;
    private int disposed;
    internal RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log)
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
    public async ValueTask<bool> Ready(CancellationToken token)
    {
        IConnection item = await Connection(token);
        return item.IsOpen;
    }
    public async ValueTask Ensure(CancellationToken token)
    {
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        await lane.ExchangeDeclareAsync(option.CommandExchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(option.DeliveryExchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(Retry(), ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(Resume(), ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(Dead(), ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        _ = await lane.QueueDeclareAsync(option.Queue, true, false, false, new Dictionary<string, object?> { ["x-dead-letter-exchange"] = Retry(), ["x-dead-letter-routing-key"] = option.Queue }, false, token);
        _ = await lane.QueueDeclareAsync(option.RetryQueue, true, false, false, new Dictionary<string, object?> { ["x-message-ttl"] = option.RetryDelaySeconds * 1000, ["x-dead-letter-exchange"] = Resume(), ["x-dead-letter-routing-key"] = option.Queue }, false, token);
        _ = await lane.QueueDeclareAsync(option.DeadQueue, true, false, false, arguments: null, noWait: false, cancellationToken: token);
        await lane.QueueBindAsync(option.Queue, option.CommandExchange, "workspace.requested", null, false, token);
        await lane.QueueBindAsync(option.Queue, Resume(), option.Queue, null, false, token);
        await lane.QueueBindAsync(option.RetryQueue, Retry(), option.Queue, null, false, token);
        await lane.QueueBindAsync(option.DeadQueue, Dead(), option.Queue, null, false, token);
    }
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }
        if (!await gate.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            IConnection? item = link;
            if (item is not null)
            {
                await Close(item);
                if (ReferenceEquals(link, item))
                {
                    link = null;
                }
            }
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
    private string Retry() => $"{option.CommandExchange}.retry";
    private string Resume() => $"{option.CommandExchange}.resume";
    private string Dead() => $"{option.CommandExchange}.dead";
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
