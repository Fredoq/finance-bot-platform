using System.Diagnostics;
using System.Text.Json;
using Finance.Application.Contracts.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using TelegramGateway.Application.Messaging;
using TelegramGateway.Infrastructure.Configuration;

namespace TelegramGateway.Infrastructure.Messaging;

internal sealed class RabbitMqBusPort(IBrokerState state, IOptions<RabbitMqOptions> option, ILogger<RabbitMqBusPort> log) : IBusPort, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IChannel? lane;
    private IConnection? link;
    private int disposed;
    /// <summary>
    /// Publishes an application envelope to RabbitMQ.
    /// </summary>
    /// <param name="message">The message envelope.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the publish finishes.</returns>
    public async ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, nameof(RabbitMqBusPort));
        await gate.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) == 1, nameof(RabbitMqBusPort));
            IConnection item = await state.Connection(token);
            if (lane is null || !lane.IsOpen || !ReferenceEquals(link, item))
            {
                await Close();
                lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
                link = item;
            }
            byte[] note = JsonSerializer.SerializeToUtf8Bytes(message);
            BasicProperties data = Properties(message);
            await lane.BasicPublishAsync(option.Value.Exchange, message.Contract, true, data, note, token);
            log.LogInformation("Application message was published");
        }
        catch (PublishException error)
        {
            await Close();
            log.LogError(error, "Message publish failed");
            throw new BusException("Message publish failed", error);
        }
        catch (OperationCanceledException error)
        {
            Cancel(error);
            throw;
        }
        catch (Exception error)
        {
            await Close();
            log.LogError(error, "Broker transport failed");
            throw new BusException("Broker transport failed", error);
        }
        finally
        {
            gate.Release();
        }
    }
    /// <summary>
    /// Disposes the cached channel resources.
    /// </summary>
    /// <returns>A task that completes when disposal finishes.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }
        await gate.WaitAsync();
        try
        {
            await Close();
        }
        finally
        {
            gate.Release();
            gate.Dispose();
        }
    }
    private static BasicProperties Properties<TMessage>(MessageEnvelope<TMessage> message) where TMessage : class => new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        MessageId = message.MessageId.ToString(),
        CorrelationId = message.CorrelationId,
        Timestamp = new AmqpTimestamp(message.OccurredUtc.ToUnixTimeSeconds()),
        Type = message.Contract,
        Headers = Headers(message)
    };
    private static Dictionary<string, object?> Headers<TMessage>(MessageEnvelope<TMessage> message) where TMessage : class
    {
        var item = new Dictionary<string, object?>
        {
            ["contract"] = message.Contract,
            ["message-id"] = message.MessageId.ToString(),
            ["correlation-id"] = message.CorrelationId,
            ["causation-id"] = message.CausationId,
            ["idempotency-key"] = message.IdempotencyKey
        };
        string? text = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(text))
        {
            item["traceparent"] = text;
        }
        return item;
    }
    private async ValueTask Close()
    {
        IChannel? item = lane;
        lane = null;
        link = null;
        if (item is null)
        {
            return;
        }
        try
        {
            if (item.IsOpen)
            {
                await item.CloseAsync();
            }
            await item.DisposeAsync();
        }
        catch (Exception error)
        {
            log.LogWarning(error, "Broker channel cleanup failed");
        }
    }
    private void Cancel(OperationCanceledException error) => log.LogInformation(error, "Broker transport cancelled");
}
