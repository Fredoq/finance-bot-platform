using FinanceCore.Infrastructure.Configuration.RabbitMq;
using FinanceCore.Infrastructure.Persistence.Postgres.Outbox;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqOutboxLoop : RabbitMqLoop
{
    private const string Publish = "Outbox publish failed";
    private const string FailureState = "Outbox failure state update failed";
    private readonly IBrokerState state;
    private readonly RabbitMqOptions option;
    private readonly PostgresOutboxPort port;
    internal RabbitMqOutboxLoop(IBrokerState state, IOptions<RabbitMqOptions> option, PostgresOutboxPort port, ILogger<RabbitMqOutboxLoop> log)
        : base(log)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        ArgumentNullException.ThrowIfNull(option);
        this.option = option.Value;
        this.port = port ?? throw new ArgumentNullException(nameof(port));
    }
    protected override string Failure() => "Outbox loop failed";
    protected override ValueTask Run(CancellationToken token) => Loop(token);
    private async ValueTask Loop(CancellationToken token)
    {
        IReadOnlyList<OutboxItem> list = await port.Items(option.OutboxBatchSize, token);
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
                await lane.BasicPublishAsync(option.DeliveryExchange, note.Head.RoutingKey, true, Properties(note), note.Body.Payload, token);
                await port.Mark(note.Head.MessageId, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                await Fail(note, error, token);
            }
        }
    }
    private static BasicProperties Properties(OutboxItem item) => new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        MessageId = item.Head.MessageId.ToString(),
        CorrelationId = item.Mark.CorrelationId,
        Timestamp = new AmqpTimestamp(item.Head.OccurredUtc.ToUnixTimeSeconds()),
        Type = item.Head.Contract,
        Headers = new Dictionary<string, object?>
        {
            ["contract"] = item.Head.Contract,
            ["message-id"] = item.Head.MessageId.ToString(),
            ["correlation-id"] = item.Mark.CorrelationId,
            ["causation-id"] = item.Mark.CausationId
        }
    };
    private async ValueTask Fail(OutboxItem item, Exception error, CancellationToken token)
    {
        try
        {
            await port.Fail(item.Head.MessageId, error.Message, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception note)
        {
            Log.LogError(note, "{Message}", FailureState);
        }
        Log.LogWarning(error, "{Message}", Publish);
    }
}
