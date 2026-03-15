using System.Diagnostics.Metrics;
using System.Text;
using TelegramGateway.Application.Telegram.Delivery;
using TelegramGateway.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqDeliveryLoop : BackgroundService
{
    private const string Attempt = "finance-attempt";
    private const string Failure = "finance-failure";
    private static readonly Meter meter = new("TelegramGateway.Delivery");
    private static readonly Counter<long> success = meter.CreateCounter<long>("telegram_gateway_delivery_success");
    private static readonly Counter<long> retry = meter.CreateCounter<long>("telegram_gateway_delivery_retry");
    private static readonly Counter<long> dead = meter.CreateCounter<long>("telegram_gateway_delivery_dead");
    private readonly IBrokerState state;
    private readonly RabbitMqOptions option;
    private readonly ITelegramDeliveryFlow flow;
    private readonly ILogger<RabbitMqDeliveryLoop> log;
    public RabbitMqDeliveryLoop(IBrokerState state, IOptions<RabbitMqOptions> option, ITelegramDeliveryFlow flow, ILogger<RabbitMqDeliveryLoop> log)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        ArgumentNullException.ThrowIfNull(option);
        this.option = option.Value;
        this.flow = flow ?? throw new ArgumentNullException(nameof(flow));
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
                log.LogError(error, "Telegram delivery loop failed");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
    private async ValueTask Loop(CancellationToken token)
    {
        IConnection item = await state.Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        await lane.BasicQosAsync(0, option.DeliveryPrefetch, false, token);
        while (!token.IsCancellationRequested)
        {
            BasicGetResult? data = await lane.BasicGetAsync(option.DeliveryQueue, false, token);
            if (data is null)
            {
                await Task.Delay(250, token);
                continue;
            }
            await Handle(lane, data, token);
        }
    }
    private async ValueTask Handle(IChannel lane, BasicGetResult data, CancellationToken token)
    {
        string contract = Header(data.BasicProperties.Headers, "contract");
        if (string.IsNullOrWhiteSpace(contract))
        {
            contract = data.BasicProperties.Type ?? string.Empty;
        }
        int attempt = Number(data.BasicProperties.Headers, option.DeliveryQueue);
        try
        {
            await flow.Run(contract, data.Body, token);
            await lane.BasicAckAsync(data.DeliveryTag, false, token);
            success.Add(1, Tags(contract, attempt));
            log.LogInformation("Telegram delivery succeeded for contract {Contract} message {MessageId} correlation {CorrelationId} attempt {Attempt}", contract, data.BasicProperties.MessageId, data.BasicProperties.CorrelationId, attempt);
        }
        catch (DeliveryException error) when (!error.Retryable)
        {
            await Dead(lane, data, attempt, error.Message, token);
            dead.Add(1, Tags(contract, attempt));
            log.LogWarning(error, "Telegram delivery moved to dead queue for contract {Contract} message {MessageId} correlation {CorrelationId} attempt {Attempt}", contract, data.BasicProperties.MessageId, data.BasicProperties.CorrelationId, attempt);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            if (attempt >= option.DeliveryMaxAttempts)
            {
                await Dead(lane, data, attempt, error.Message, token);
                dead.Add(1, Tags(contract, attempt));
                log.LogError(error, "Telegram delivery exhausted retry budget for contract {Contract} message {MessageId} correlation {CorrelationId} attempt {Attempt}", contract, data.BasicProperties.MessageId, data.BasicProperties.CorrelationId, attempt);
                return;
            }
            await lane.BasicRejectAsync(data.DeliveryTag, false, token);
            retry.Add(1, Tags(contract, attempt));
            log.LogWarning(error, "Telegram delivery moved to retry queue for contract {Contract} message {MessageId} correlation {CorrelationId} attempt {Attempt}", contract, data.BasicProperties.MessageId, data.BasicProperties.CorrelationId, attempt);
        }
    }
    private async ValueTask Dead(IChannel lane, BasicGetResult data, int attempt, string error, CancellationToken token)
    {
        BasicProperties item = Properties(data, attempt, error);
        await lane.BasicPublishAsync($"{option.DeliveryExchange}.dead", option.DeliveryQueue, true, item, data.Body, token);
        await lane.BasicAckAsync(data.DeliveryTag, false, token);
    }
    private static BasicProperties Properties(BasicGetResult data, int attempt, string error) => new()
    {
        ContentType = string.IsNullOrWhiteSpace(data.BasicProperties.ContentType) ? "application/json" : data.BasicProperties.ContentType,
        DeliveryMode = DeliveryModes.Persistent,
        MessageId = data.BasicProperties.MessageId,
        CorrelationId = data.BasicProperties.CorrelationId,
        Timestamp = data.BasicProperties.Timestamp,
        Type = data.BasicProperties.Type,
        Headers = Headers(data.BasicProperties.Headers, attempt, error)
    };
    private static Dictionary<string, object?> Headers(IDictionary<string, object?>? source, int attempt, string error)
    {
        var item = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (source is not null)
        {
            foreach ((string key, object? value) in source)
            {
                item[key] = value;
            }
        }
        item[Attempt] = attempt;
        item[Failure] = error;
        return item;
    }
    private static KeyValuePair<string, object?>[] Tags(string contract, int attempt) => [new("contract", contract), new("attempt", attempt)];
    private static string Header(IDictionary<string, object?>? source, string key)
    {
        if (source is null || !source.TryGetValue(key, out object? value) || value is null)
        {
            return string.Empty;
        }
        return value switch
        {
            string item => item,
            byte[] item => Encoding.UTF8.GetString(item),
            _ => value.ToString() ?? string.Empty
        };
    }
    private static int Number(IDictionary<string, object?>? source, string queue)
    {
        if (source is null || !source.TryGetValue(Attempt, out object? value) || value is null)
        {
            int death = Death(source, queue);
            return death > 0 ? death + 1 : 1;
        }
        int item = Count(value);
        return item > 0 ? item : Death(source, queue) + 1;
    }
    private static int Death(IDictionary<string, object?>? source, string queue)
    {
        if (source is null || !source.TryGetValue("x-death", out object? value) || value is null || value is not IList<object> list)
        {
            return 0;
        }
        foreach (object? item in list)
        {
            if (item is IDictionary<string, object?> note && string.Equals(Header(note, "queue"), queue, StringComparison.Ordinal))
            {
                return Count(note.TryGetValue("count", out object? count) ? count : null);
            }
            if (item is IDictionary<string, object> data)
            {
                var head = data.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
                if (string.Equals(Header(head, "queue"), queue, StringComparison.Ordinal))
                {
                    return Count(head.TryGetValue("count", out object? count) ? count : null);
                }
            }
        }
        return 0;
    }
    private static int Count(object? value) => value switch
    {
        int item => item,
        long item => (int)item,
        byte item => item,
        byte[] item when int.TryParse(Encoding.UTF8.GetString(item), out int note) => note,
        string item when int.TryParse(item, out int note) => note,
        _ => 0
    };
}
