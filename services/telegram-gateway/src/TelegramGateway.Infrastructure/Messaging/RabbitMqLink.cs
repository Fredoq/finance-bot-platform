using Finance.Platform.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqLink : RabbitMqState<RabbitMqOptions>, IBrokerState
{
    private readonly RabbitMqExchanges exchange;
    public RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log) : base(option?.Value ?? throw new ArgumentNullException(nameof(option)), log) => exchange = new(Option.CommandExchange, Option.DeliveryExchange, $"{Option.DeliveryExchange}.retry", $"{Option.DeliveryExchange}.resume", $"{Option.DeliveryExchange}.dead");
    public async ValueTask Ensure(CancellationToken token)
    {
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        RabbitMqQueues queue = new(Option.DeliveryQueue, Option.DeliveryRetryQueue, Option.DeliveryDeadQueue, Option.DeliveryRetryDelaySeconds, Option.DeliveryQueue, Option.DeliveryDeadQueue);
        await RabbitMqTopology.Declare(lane, exchange, token);
        await RabbitMqTopology.Queue(lane, exchange, queue, token);
        await lane.QueueBindAsync(Option.DeliveryQueue, Option.DeliveryExchange, "#", null, false, token);
    }
}
