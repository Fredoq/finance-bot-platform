using System.ComponentModel.DataAnnotations;
using Finance.Platform.RabbitMq;

namespace FinanceCore.Infrastructure.Configuration.RabbitMq;

/// <summary>
/// Represents the RabbitMQ settings required by the finance core runtime.
/// </summary>
public sealed class RabbitMqOptions : RabbitMqConnectionOptions, IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "RabbitMq";
    /// <summary>
    /// Gets or sets the inbound command exchange name.
    /// </summary>
    public string CommandExchange { get; init; } = "finance.command";
    /// <summary>
    /// Gets or sets the outbound delivery exchange name.
    /// </summary>
    public string DeliveryExchange { get; init; } = "finance.delivery";
    /// <summary>
    /// Gets or sets the main queue name.
    /// </summary>
    public string Queue { get; init; } = "finance-core.command";
    /// <summary>
    /// Gets or sets the retry queue name.
    /// </summary>
    public string RetryQueue { get; init; } = "finance-core.command.retry";
    /// <summary>
    /// Gets or sets the dead queue name.
    /// </summary>
    public string DeadQueue { get; init; } = "finance-core.command.dead";
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public override string Client { get; init; } = "finance-core";
    /// <summary>
    /// Gets or sets the prefetch count.
    /// </summary>
    public ushort Prefetch { get; init; } = 16;
    /// <summary>
    /// Gets or sets the retry delay in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 30;
    /// <summary>
    /// Gets or sets the maximum delivery attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 5;
    /// <summary>
    /// Gets or sets the outbox publish batch size.
    /// </summary>
    public int OutboxBatchSize { get; init; } = 32;
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
        RabbitMqValidation.Require(list, Queue, nameof(Queue), "RabbitMq queue is required");
        RabbitMqValidation.Require(list, RetryQueue, nameof(RetryQueue), "RabbitMq retry queue is required");
        RabbitMqValidation.Require(list, DeadQueue, nameof(DeadQueue), "RabbitMq dead queue is required");
        RabbitMqValidation.Distinct(list, Queue, RetryQueue, nameof(Queue), nameof(RetryQueue), "RabbitMq queue and retry queue must be distinct");
        RabbitMqValidation.Distinct(list, Queue, DeadQueue, nameof(Queue), nameof(DeadQueue), "RabbitMq queue and dead queue must be distinct");
        RabbitMqValidation.Distinct(list, RetryQueue, DeadQueue, nameof(RetryQueue), nameof(DeadQueue), "RabbitMq retry queue and dead queue must be distinct");
        RabbitMqValidation.Positive(list, Prefetch, nameof(Prefetch), "RabbitMq prefetch must be greater than zero");
        RabbitMqValidation.Positive(list, RetryDelaySeconds, nameof(RetryDelaySeconds), "RabbitMq retry delay must be greater than zero");
        RabbitMqValidation.Positive(list, MaxAttempts, nameof(MaxAttempts), "RabbitMq max attempts must be greater than zero");
        RabbitMqValidation.Positive(list, OutboxBatchSize, nameof(OutboxBatchSize), "RabbitMq outbox batch size must be greater than zero");
        return list;
    }
}
