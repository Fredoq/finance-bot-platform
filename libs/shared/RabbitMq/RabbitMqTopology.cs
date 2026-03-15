using RabbitMQ.Client;

namespace Finance.Platform.RabbitMq;

internal static class RabbitMqTopology
{
    internal static async ValueTask Declare(IChannel lane, string commandExchange, string deliveryExchange, string retryExchange, string resumeExchange, string deadExchange, CancellationToken token)
    {
        await lane.ExchangeDeclareAsync(commandExchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(deliveryExchange, ExchangeType.Topic, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(retryExchange, ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(resumeExchange, ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
        await lane.ExchangeDeclareAsync(deadExchange, ExchangeType.Direct, true, false, arguments: null, cancellationToken: token);
    }
    internal static async ValueTask Queue(IChannel lane, string queue, string retryQueue, string deadQueue, int retryDelaySeconds, string retryExchange, string resumeExchange, string deadExchange, string liveRoutingKey, string deadRoutingKey, CancellationToken token)
    {
        _ = await lane.QueueDeclareAsync(queue, true, false, false, new Dictionary<string, object?> { ["x-dead-letter-exchange"] = retryExchange, ["x-dead-letter-routing-key"] = liveRoutingKey }, false, token);
        _ = await lane.QueueDeclareAsync(retryQueue, true, false, false, new Dictionary<string, object?> { ["x-message-ttl"] = retryDelaySeconds * 1000, ["x-dead-letter-exchange"] = resumeExchange, ["x-dead-letter-routing-key"] = liveRoutingKey }, false, token);
        _ = await lane.QueueDeclareAsync(deadQueue, true, false, false, arguments: null, noWait: false, cancellationToken: token);
        await lane.QueueBindAsync(queue, resumeExchange, liveRoutingKey, null, false, token);
        await lane.QueueBindAsync(retryQueue, retryExchange, liveRoutingKey, null, false, token);
        await lane.QueueBindAsync(deadQueue, deadExchange, deadRoutingKey, null, false, token);
    }
}
