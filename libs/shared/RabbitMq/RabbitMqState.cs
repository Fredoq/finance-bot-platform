using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Finance.Platform.RabbitMq;

internal abstract class RabbitMqState<TOptions> : IAsyncDisposable where TOptions : RabbitMqConnectionOptions
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConnectionFactory factory;
    private readonly ILogger log;
    private IConnection? link;
    private int disposed;
    protected RabbitMqState(TOptions option, ILogger log)
    {
        Option = option ?? throw new ArgumentNullException(nameof(option));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        factory = string.IsNullOrWhiteSpace(Option.ConnectionString) ? new ConnectionFactory
        {
            HostName = Option.Host,
            Port = Option.Port,
            VirtualHost = Option.VirtualHost,
            UserName = Option.Username,
            Password = Option.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = Option.Client,
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        } : new ConnectionFactory
        {
            Uri = new Uri(Option.ConnectionString, UriKind.Absolute),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = Option.Client,
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        };
    }
    protected TOptions Option { get; }
    public async ValueTask<IConnection> Connection(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, GetType().Name);
        if (link is { IsOpen: true })
        {
            return link;
        }
        await gate.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, GetType().Name);
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
