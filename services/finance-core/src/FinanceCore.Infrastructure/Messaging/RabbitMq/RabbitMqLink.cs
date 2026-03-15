using Finance.Platform.RabbitMq;
using FinanceCore.Infrastructure.Configuration.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqLink : RabbitMqState<RabbitMqOptions>, IBrokerState
{
    internal RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log) : base(option?.Value ?? throw new ArgumentNullException(nameof(option)), log)
    {
    }
    public async ValueTask Ensure(CancellationToken token)
    {
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        await RabbitMqTopology.Declare(lane, Option.CommandExchange, Option.DeliveryExchange, Retry(), Resume(), Dead(), token);
        await RabbitMqTopology.Queue(lane, Option.Queue, Option.RetryQueue, Option.DeadQueue, Option.RetryDelaySeconds, Retry(), Resume(), Dead(), Option.Queue, Option.Queue, token);
        await lane.QueueBindAsync(Option.Queue, Option.CommandExchange, "workspace.requested", null, false, token);
    }
    private string Retry() => $"{Option.CommandExchange}.retry";
    private string Resume() => $"{Option.CommandExchange}.resume";
    private string Dead() => $"{Option.CommandExchange}.dead";
}
