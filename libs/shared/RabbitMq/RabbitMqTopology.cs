using RabbitMQ.Client;

namespace Finance.Platform.RabbitMq;

internal static class RabbitMqTopology
{
    internal static async ValueTask Declare(IChannel lane, RabbitMqExchanges item, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(item);
        await lane.ExchangeDeclareAsync(item.Command, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(item.Delivery, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(item.Retry, ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(item.Resume, ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(item.Dead, ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
    }
    internal static async ValueTask Queue(IChannel lane, RabbitMqExchanges exchange, RabbitMqQueues queue, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        ArgumentNullException.ThrowIfNull(queue);
        _ = await lane.QueueDeclareAsync(queue.Live, true, false, false, new Dictionary<string, object?> { ["x-dead-letter-exchange"] = exchange.Retry, ["x-dead-letter-routing-key"] = queue.LiveRouting }, false, token);
        _ = await lane.QueueDeclareAsync(queue.Retry, true, false, false, new Dictionary<string, object?> { ["x-message-ttl"] = queue.RetryDelaySeconds * 1000, ["x-dead-letter-exchange"] = exchange.Resume, ["x-dead-letter-routing-key"] = queue.LiveRouting }, false, token);
        _ = await lane.QueueDeclareAsync(queue.Dead, true, false, false, arguments: null, noWait: false, cancellationToken: token);
        await lane.QueueBindAsync(queue.Live, exchange.Resume, queue.LiveRouting, null, false, token);
        await lane.QueueBindAsync(queue.Retry, exchange.Retry, queue.LiveRouting, null, false, token);
        await lane.QueueBindAsync(queue.Dead, exchange.Dead, queue.DeadRouting, null, false, token);
    }
}
