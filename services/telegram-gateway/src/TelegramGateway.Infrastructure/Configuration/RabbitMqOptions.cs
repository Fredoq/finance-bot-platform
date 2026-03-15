using System.ComponentModel.DataAnnotations;
using Finance.Platform.RabbitMq;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the RabbitMQ settings required by the gateway transport.
/// </summary>
public sealed class RabbitMqOptions : RabbitMqConnectionOptions, IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "RabbitMq";
    /// <summary>
    /// Gets or sets the ingress command exchange name.
    /// </summary>
    public string CommandExchange { get; init; } = "finance.command";
    /// <summary>
    /// Gets or sets the outbound delivery exchange name.
    /// </summary>
    public string DeliveryExchange { get; init; } = "finance.delivery";
    /// <summary>
    /// Gets or sets the delivery queue name.
    /// </summary>
    public string DeliveryQueue { get; init; } = "telegram-gateway.delivery";
    /// <summary>
    /// Gets or sets the delivery retry queue name.
    /// </summary>
    public string DeliveryRetryQueue { get; init; } = "telegram-gateway.delivery.retry";
    /// <summary>
    /// Gets or sets the delivery dead queue name.
    /// </summary>
    public string DeliveryDeadQueue { get; init; } = "telegram-gateway.delivery.dead";
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public override string Client { get; init; } = "telegram-gateway";
    /// <summary>
    /// Gets or sets the delivery prefetch count.
    /// </summary>
    public ushort DeliveryPrefetch { get; init; } = 16;
    /// <summary>
    /// Gets or sets the delivery retry delay in seconds.
    /// </summary>
    public int DeliveryRetryDelaySeconds { get; init; } = 30;
    /// <summary>
    /// Gets or sets the maximum delivery attempts.
    /// </summary>
    public int DeliveryMaxAttempts { get; init; } = 5;
    /// <summary>
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        var list = new List<ValidationResult>();
        RabbitMqValidation.Connection(list, this);
        RabbitMqValidation.Require(list, CommandExchange, nameof(CommandExchange), "RabbitMq command exchange is required");
        RabbitMqValidation.Require(list, DeliveryExchange, nameof(DeliveryExchange), "RabbitMq delivery exchange is required");
        RabbitMqValidation.Distinct(list, CommandExchange, DeliveryExchange, nameof(CommandExchange), nameof(DeliveryExchange), "RabbitMq command exchange and delivery exchange must be distinct");
        RabbitMqValidation.Require(list, DeliveryQueue, nameof(DeliveryQueue), "RabbitMq delivery queue is required");
        RabbitMqValidation.Require(list, DeliveryRetryQueue, nameof(DeliveryRetryQueue), "RabbitMq delivery retry queue is required");
        RabbitMqValidation.Require(list, DeliveryDeadQueue, nameof(DeliveryDeadQueue), "RabbitMq delivery dead queue is required");
        RabbitMqValidation.Distinct(list, DeliveryQueue, DeliveryRetryQueue, nameof(DeliveryQueue), nameof(DeliveryRetryQueue), "RabbitMq delivery queue and retry queue must be distinct");
        RabbitMqValidation.Distinct(list, DeliveryQueue, DeliveryDeadQueue, nameof(DeliveryQueue), nameof(DeliveryDeadQueue), "RabbitMq delivery queue and dead queue must be distinct");
        RabbitMqValidation.Distinct(list, DeliveryRetryQueue, DeliveryDeadQueue, nameof(DeliveryRetryQueue), nameof(DeliveryDeadQueue), "RabbitMq delivery retry queue and dead queue must be distinct");
        RabbitMqValidation.Positive(list, DeliveryPrefetch, nameof(DeliveryPrefetch), "RabbitMq delivery prefetch must be greater than zero");
        RabbitMqValidation.Positive(list, DeliveryRetryDelaySeconds, nameof(DeliveryRetryDelaySeconds), "RabbitMq delivery retry delay must be greater than zero");
        RabbitMqValidation.Positive(list, DeliveryMaxAttempts, nameof(DeliveryMaxAttempts), "RabbitMq delivery max attempts must be greater than zero");
        return list;
    }
}
