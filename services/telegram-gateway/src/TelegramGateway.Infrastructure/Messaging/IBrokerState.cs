using RabbitMQ.Client;

namespace TelegramGateway.Infrastructure.Messaging;

internal interface IBrokerState
{
    ValueTask<IConnection> Connection(CancellationToken token);
    ValueTask<bool> Ready(CancellationToken token);
    ValueTask Ensure(CancellationToken token);
}
