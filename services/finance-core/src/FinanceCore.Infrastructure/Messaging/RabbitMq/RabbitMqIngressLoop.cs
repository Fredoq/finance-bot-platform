using System.Text;
using FinanceCore.Application.Runtime.Faults;
using FinanceCore.Application.Runtime.Flow;
using FinanceCore.Infrastructure.Configuration.RabbitMq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FinanceCore.Infrastructure.Messaging.RabbitMq;

internal sealed class RabbitMqIngressLoop : RabbitMqLoop
{
    private const string Attempt = "finance-attempt";
    private const string FailureHeader = "finance-failure";
    private readonly IBrokerState state;
    private readonly RabbitMqOptions option;
    private readonly ICommandFlow flow;
    internal RabbitMqIngressLoop(IBrokerState state, IOptions<RabbitMqOptions> option, ICommandFlow flow, ILogger<RabbitMqIngressLoop> log)
        : base(log)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        ArgumentNullException.ThrowIfNull(option);
        this.option = option.Value;
        this.flow = flow ?? throw new ArgumentNullException(nameof(flow));
    }
    protected override string Failure() => "Ingress loop failed";
    protected override ValueTask Run(CancellationToken token) => Loop(token);
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
        int attempt = Number(data.BasicProperties.Headers, option.Queue);
        try
        {
            await flow.Run(contract, data.Body, token);
            await lane.BasicAckAsync(data.DeliveryTag, false, token);
        }
        catch (InvalidMessageException error)
        {
            await Dead(lane, data, attempt, error.Message, token);
            Log.LogWarning(error, "Command moved to dead queue");
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
                Log.LogError(error, "Command exhausted retry budget");
                return;
            }
            await Retry(lane, data, token);
            Log.LogWarning(error, "Command moved to retry queue");
        }
    }
    private static ValueTask Retry(IChannel lane, BasicGetResult data, CancellationToken token) => lane.BasicRejectAsync(data.DeliveryTag, false, token);
    private async ValueTask Dead(IChannel lane, BasicGetResult data, int attempt, string error, CancellationToken token)
    {
        BasicProperties item = Properties(data, attempt, error);
        await lane.BasicPublishAsync($"{option.CommandExchange}.dead", option.Queue, true, item, data.Body, token);
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
        item[FailureHeader] = error;
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
    private static int Number(IDictionary<string, object?>? source, string queue)
    {
        if (source is null || !source.TryGetValue(Attempt, out object? value) || value is null)
        {
            int data = Death(source, queue);
            return data > 0 ? data + 1 : 1;
        }
        int death = Death(source, queue);
        return value switch
        {
            int item => item,
            long item => (int)item,
            byte item => item,
            byte[] item when int.TryParse(Encoding.UTF8.GetString(item), out int note) => note,
            string item when int.TryParse(item, out int data) => data,
            _ => death > 0 ? death + 1 : 1
        };
    }
    private static int Death(IDictionary<string, object?>? source, string queue)
    {
        if (source is null || !source.TryGetValue("x-death", out object? value) || value is null)
        {
            return 0;
        }
        if (value is IList<object> list)
        {
            foreach (object? item in list)
            {
                if (item is IDictionary<string, object?> note && string.Equals(Header(note, "queue"), queue, StringComparison.Ordinal))
                {
                    return Count(note, "count");
                }
                if (item is IDictionary<string, object> data && string.Equals(Header(Copy(data), "queue"), queue, StringComparison.Ordinal))
                {
                    return Count(Copy(data), "count");
                }
            }
        }
        return 0;
    }
    private static int Count(IDictionary<string, object?> source, string key) => source.TryGetValue(key, out object? value) && value is not null ? value switch
    {
        int item => item,
        long item => (int)item,
        byte item => item,
        byte[] item when int.TryParse(Encoding.UTF8.GetString(item), out int note) => note,
        string item when int.TryParse(item, out int data) => data,
        _ => 0
    } : 0;
    private static Dictionary<string, object?> Copy(IDictionary<string, object> source) => source.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
}
