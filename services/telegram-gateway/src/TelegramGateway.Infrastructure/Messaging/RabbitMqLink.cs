using Finance.Platform.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqLink : RabbitMqState<RabbitMqOptions>, IBrokerState
{
    public RabbitMqLink(IOptions<RabbitMqOptions> option, ILogger<RabbitMqLink> log) : base(option?.Value ?? throw new ArgumentNullException(nameof(option)), log)
    {
    }
    public async ValueTask Ensure(CancellationToken token)
    {
        IConnection item = await Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        await RabbitMqTopology.Declare(lane, Option.CommandExchange, Option.DeliveryExchange, Retry(), Resume(), Dead(), token);
        await RabbitMqTopology.Queue(lane, Option.DeliveryQueue, Option.DeliveryRetryQueue, Option.DeliveryDeadQueue, Option.DeliveryRetryDelaySeconds, Retry(), Resume(), Dead(), Option.DeliveryQueue, Option.DeliveryDeadQueue, token);
        await lane.QueueBindAsync(Option.DeliveryQueue, Option.DeliveryExchange, "#", null, false, token);
    }
    private string Retry() => $"{Option.DeliveryExchange}.retry";
    private string Resume() => $"{Option.DeliveryExchange}.resume";
    private string Dead() => $"{Option.DeliveryExchange}.dead";
}
