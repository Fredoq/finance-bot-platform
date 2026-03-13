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
internal sealed class RabbitMqBusPort(IBrokerState state, IOptions<RabbitMqOptions> option, ILogger<RabbitMqBusPort> log) : IBusPort
{
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
        try
        {
            await state.Ensure(token);
            var item = await state.Connection(token);
            await using var lane = await item.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: token);
            var note = JsonSerializer.SerializeToUtf8Bytes(message);
            var data = Properties(message);
            await lane.BasicPublishAsync(option.Value.Exchange, message.Contract, true, data, note, token);
            log.LogInformation("Application message was published");
        }
        catch (PublishException error)
        {
            log.LogError(error, "Message publish failed");
            throw new BusException("Message publish failed", error);
        }
        catch (Exception error)
        {
            log.LogError(error, "Broker transport failed");
            throw new BusException("Broker transport failed", error);
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
    private BasicProperties Properties<TMessage>(MessageEnvelope<TMessage> message) where TMessage : class
    {
        return new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = message.MessageId.ToString(),
            CorrelationId = message.CorrelationId,
            Timestamp = new AmqpTimestamp(message.OccurredUtc.ToUnixTimeSeconds()),
            Type = message.Contract,
            Headers = Headers(message)
        };
    }
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
        var text = Activity.Current?.Id;
        if (!string.IsNullOrWhiteSpace(text))
        {
            item["traceparent"] = text;
        }
        return item;
    }
}
