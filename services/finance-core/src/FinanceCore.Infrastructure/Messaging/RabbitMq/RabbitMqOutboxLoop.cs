using FinanceCore.Infrastructure.Configuration.RabbitMq;
using FinanceCore.Infrastructure.Persistence.Postgres.Outbox;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqOutboxLoop : BackgroundService
{
    private readonly IBrokerState state;
    private readonly RabbitMqOptions option;
    private readonly PostgresOutboxPort port;
    private readonly ILogger<RabbitMqOutboxLoop> log;
    internal RabbitMqOutboxLoop(IBrokerState state, IOptions<RabbitMqOptions> option, PostgresOutboxPort port, ILogger<RabbitMqOutboxLoop> log)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        ArgumentNullException.ThrowIfNull(option);
        this.option = option.Value;
        this.port = port ?? throw new ArgumentNullException(nameof(port));
        this.log = log ?? throw new ArgumentNullException(nameof(log));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Loop(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                log.LogError(error, "Outbox loop failed");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
    private async ValueTask Loop(CancellationToken token)
    {
        IReadOnlyList<OutboxItem> list = await port.Items(32, token);
        if (list.Count == 0)
        {
            await Task.Delay(250, token);
            return;
        }
        IConnection item = await state.Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        foreach (OutboxItem note in list)
        {
            try
            {
                await lane.BasicPublishAsync(option.Exchange, note.RoutingKey, true, Properties(note), note.Payload, token);
                await port.Mark(note.MessageId, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (PublishException error)
            {
                await port.Fail(note.MessageId, error.Message, token);
                log.LogWarning(error, "Outbox publish failed");
            }
        }
    }
    private static BasicProperties Properties(OutboxItem item) => new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        MessageId = item.MessageId.ToString(),
        CorrelationId = item.CorrelationId,
        Timestamp = new AmqpTimestamp(item.OccurredUtc.ToUnixTimeSeconds()),
        Type = item.Contract,
        Headers = new Dictionary<string, object?>
        {
            ["contract"] = item.Contract,
            ["message-id"] = item.MessageId.ToString(),
            ["correlation-id"] = item.CorrelationId,
            ["causation-id"] = item.CausationId
        }
    };
}
