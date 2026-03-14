using FinanceCore.Application.Runtime.Faults;
using FinanceCore.Application.Runtime.Flow;
using FinanceCore.Infrastructure.Configuration.RabbitMq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqIngressLoop : BackgroundService
{
    private const string Attempt = "finance-attempt";
    private const string Failure = "finance-failure";
    private readonly IBrokerState state;
    private readonly RabbitMqOptions option;
    private readonly ICommandFlow flow;
    private readonly ILogger<RabbitMqIngressLoop> log;
    internal RabbitMqIngressLoop(IBrokerState state, IOptions<RabbitMqOptions> option, ICommandFlow flow, ILogger<RabbitMqIngressLoop> log)
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
                log.LogError(error, "Command loop failed");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
    private async ValueTask Loop(CancellationToken token)
    {
        IConnection item = await state.Connection(token);
        await using IChannel lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
        await lane.BasicQosAsync(0, option.Prefetch, false, token);
        while (!token.IsCancellationRequested)
        {
            BasicGetResult? data = await lane.BasicGetAsync(option.Queue, false, token);
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
        string contract = !string.IsNullOrWhiteSpace(data.BasicProperties.Type) ? data.BasicProperties.Type : Header(data.BasicProperties.Headers, "contract");
        int attempt = Number(data.BasicProperties.Headers);
        try
        {
            await flow.Run(contract, data.Body, token);
            await lane.BasicAckAsync(data.DeliveryTag, false, token);
        }
        catch (InvalidMessageException error)
        {
            await Dead(lane, data, attempt, error.Message, token);
            await lane.BasicAckAsync(data.DeliveryTag, false, token);
            log.LogWarning(error, "Command moved to dead queue");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            if (attempt >= option.MaxAttempts)
            {
                await Dead(lane, data, attempt, error.Message, token);
                await lane.BasicAckAsync(data.DeliveryTag, false, token);
                log.LogError(error, "Command exhausted retry budget");
                return;
            }
            await Retry(lane, data, attempt + 1, error.Message, token);
            await lane.BasicAckAsync(data.DeliveryTag, false, token);
            log.LogWarning(error, "Command moved to retry queue");
        }
    }
    private async ValueTask Retry(IChannel lane, BasicGetResult data, int attempt, string error, CancellationToken token)
    {
        BasicProperties item = Properties(data, attempt, error);
        await lane.BasicPublishAsync($"{option.Exchange}.retry", option.Queue, true, item, data.Body, token);
    }
    private async ValueTask Dead(IChannel lane, BasicGetResult data, int attempt, string error, CancellationToken token)
    {
        BasicProperties item = Properties(data, attempt, error);
        await lane.BasicPublishAsync($"{option.Exchange}.dead", option.Queue, true, item, data.Body, token);
    }
    private static BasicProperties Properties(BasicGetResult data, int attempt, string error)
    {
        var item = new BasicProperties
        {
            ContentType = string.IsNullOrWhiteSpace(data.BasicProperties.ContentType) ? "application/json" : data.BasicProperties.ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = data.BasicProperties.MessageId,
            CorrelationId = data.BasicProperties.CorrelationId,
            Timestamp = data.BasicProperties.Timestamp,
            Type = data.BasicProperties.Type,
            Headers = Headers(data.BasicProperties.Headers, attempt, error)
        };
        return item;
    }
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
    private static string Header(IDictionary<string, object?>? source, string key)
    {
        if (source is null || !source.TryGetValue(key, out object? value) || value is null)
        {
            return string.Empty;
        }
        return value switch
        {
            string item => item,
            byte[] item => System.Text.Encoding.UTF8.GetString(item),
            _ => value.ToString() ?? string.Empty
        };
    }
    private static int Number(IDictionary<string, object?>? source)
    {
        if (source is null || !source.TryGetValue(Attempt, out object? value) || value is null)
        {
            return 1;
        }
        return value switch
        {
            int item => item,
            long item => (int)item,
            byte item => item,
            byte[] item when item.Length > 0 => item[0],
            string item when int.TryParse(item, out int data) => data,
            _ => 1
        };
    }
}
