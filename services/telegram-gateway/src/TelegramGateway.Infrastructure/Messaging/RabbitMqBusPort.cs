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

/// <summary>
/// Publishes application messages to RabbitMQ.
/// Example:
/// <code>
/// await port.Publish(message, token);
/// </code>
/// </summary>
internal sealed class RabbitMqBusPort(IBrokerState state, IOptions<RabbitMqOptions> option, ILogger<RabbitMqBusPort> log) : IBusPort, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IChannel? lane;
    private IConnection? link;
    private int disposed;
    /// <summary>
    /// Publishes the application message envelope to RabbitMQ.
    /// Example:
    /// <code>
    /// await port.Publish(message, token);
    /// </code>
    /// </summary>
    /// <param name="message">The envelope to publish.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A task that completes when the broker confirms the publish.</returns>
    public async ValueTask Publish<TMessage>(MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        if (Volatile.Read(ref disposed) == 1)
        {
            throw new ObjectDisposedException(nameof(RabbitMqBusPort));
        }
        await gate.WaitAsync(token);
        try
        {
            if (Volatile.Read(ref disposed) == 1)
            {
                throw new ObjectDisposedException(nameof(RabbitMqBusPort));
            }
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
        catch (OperationCanceledException error) when (Cancel(error))
        {
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
    /// Disposes the cached broker channel.
    /// Example:
    /// <code>
    /// await port.DisposeAsync();
    /// </code>
    /// </summary>
    /// <returns>A task that completes when the channel is disposed.</returns>
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
    /// <summary>
    /// Builds the AMQP message properties.
    /// Example:
    /// <code>
    /// BasicProperties item = Properties(message);
    /// </code>
    /// </summary>
    /// <param name="message">The envelope to publish.</param>
    /// <returns>The AMQP properties.</returns>
    private BasicProperties Properties<TMessage>(MessageEnvelope<TMessage> message) where TMessage : class => new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        MessageId = message.MessageId.ToString(),
        CorrelationId = message.CorrelationId,
        Timestamp = new AmqpTimestamp(message.OccurredUtc.ToUnixTimeSeconds()),
        Type = message.Contract,
        Headers = Headers(message)
    };
    /// <summary>
    /// Builds the AMQP header dictionary.
    /// Example:
    /// <code>
    /// IDictionary&lt;string, object?&gt; item = Headers(message);
    /// </code>
    /// </summary>
    /// <param name="message">The envelope to publish.</param>
    /// <returns>The header dictionary.</returns>
    private static IDictionary<string, object?> Headers<TMessage>(MessageEnvelope<TMessage> message) where TMessage : class
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
    /// <summary>
    /// Closes and disposes the cached broker channel.
    /// Example:
    /// <code>
    /// await port.Close();
    /// </code>
    /// </summary>
    /// <returns>A task that completes when the channel state is cleared.</returns>
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
    /// <summary>
    /// Logs broker transport cancellation and keeps the original exception flow.
    /// Example:
    /// <code>
    /// _ = port.Cancel(error);
    /// </code>
    /// </summary>
    /// <param name="error">The cancellation exception.</param>
    /// <returns><see langword="false"/> so the exception is rethrown by the runtime.</returns>
    private bool Cancel(OperationCanceledException error)
    {
        log.LogInformation(error, "Broker transport cancelled");
        return false;
    }
}
