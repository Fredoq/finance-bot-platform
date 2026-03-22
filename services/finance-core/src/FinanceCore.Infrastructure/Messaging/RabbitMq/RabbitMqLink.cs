using Finance.Platform.RabbitMq;
using FinanceCore.Infrastructure.Configuration.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqLink : RabbitMqState<RabbitMqOptions>, IBrokerState
{
    private readonly RabbitMqExchanges exchange;
    internal RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log) : base(option?.Value ?? throw new ArgumentNullException(nameof(option)), log) => exchange = new(Option.CommandExchange, Option.DeliveryExchange, $"{Option.CommandExchange}.retry", $"{Option.CommandExchange}.resume", $"{Option.CommandExchange}.dead");
    public async ValueTask Ensure(CancellationToken token)
    {
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        RabbitMqQueues queue = new(Option.Queue, Option.RetryQueue, Option.DeadQueue, Option.RetryDelaySeconds, Option.Queue, Option.Queue);
        await RabbitMqTopology.Declare(lane, exchange, token);
        await RabbitMqTopology.Queue(lane, exchange, queue, token);
        await lane.QueueBindAsync(Option.Queue, Option.CommandExchange, "workspace.requested", null, false, token);
        await lane.QueueBindAsync(Option.Queue, Option.CommandExchange, "workspace.input.requested", null, false, token);
    }
}
