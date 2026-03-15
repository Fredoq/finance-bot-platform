using RabbitMQ.Client;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal interface IBrokerState
{
    ValueTask<IConnection> Connection(CancellationToken token);
    ValueTask<bool> Ready(CancellationToken token);
    ValueTask Ensure(CancellationToken token);
}
