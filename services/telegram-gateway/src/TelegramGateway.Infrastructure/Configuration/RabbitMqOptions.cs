using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the RabbitMQ settings required by the gateway transport.
/// </summary>
public sealed class RabbitMqOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "RabbitMq";
    /// <summary>
    /// Gets or sets the broker host name.
    /// </summary>
    public string Host { get; init; } = "localhost";
    /// <summary>
    /// Gets or sets the broker port.
    /// </summary>
    public int Port { get; init; } = 5672;
    /// <summary>
    /// Gets or sets the broker virtual host.
    /// </summary>
    public string VirtualHost { get; init; } = "/";
    /// <summary>
    /// Gets or sets the broker user name.
    /// </summary>
    public string Username { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the broker password.
    /// </summary>
    public string Password { get; init; } = string.Empty;
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
    public string Client { get; init; } = "telegram-gateway";
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
        Require(list, Host, nameof(Host), "RabbitMq host is required");
        Range(list, Port, 1, 65535, nameof(Port), "RabbitMq port must be between 1 and 65535");
        Require(list, VirtualHost, nameof(VirtualHost), "RabbitMq virtual host is required");
        Require(list, Username, nameof(Username), "RabbitMq username is required");
        Require(list, Password, nameof(Password), "RabbitMq password is required");
        Require(list, CommandExchange, nameof(CommandExchange), "RabbitMq command exchange is required");
        Require(list, DeliveryExchange, nameof(DeliveryExchange), "RabbitMq delivery exchange is required");
        Require(list, DeliveryQueue, nameof(DeliveryQueue), "RabbitMq delivery queue is required");
        Require(list, DeliveryRetryQueue, nameof(DeliveryRetryQueue), "RabbitMq delivery retry queue is required");
        Require(list, DeliveryDeadQueue, nameof(DeliveryDeadQueue), "RabbitMq delivery dead queue is required");
        Distinct(list, DeliveryQueue, DeliveryRetryQueue, nameof(DeliveryQueue), nameof(DeliveryRetryQueue), "RabbitMq delivery queue and retry queue must be distinct");
        Distinct(list, DeliveryQueue, DeliveryDeadQueue, nameof(DeliveryQueue), nameof(DeliveryDeadQueue), "RabbitMq delivery queue and dead queue must be distinct");
        Distinct(list, DeliveryRetryQueue, DeliveryDeadQueue, nameof(DeliveryRetryQueue), nameof(DeliveryDeadQueue), "RabbitMq delivery retry queue and dead queue must be distinct");
        Require(list, Client, nameof(Client), "RabbitMq client is required");
        Positive(list, DeliveryPrefetch, nameof(DeliveryPrefetch), "RabbitMq delivery prefetch must be greater than zero");
        Positive(list, DeliveryRetryDelaySeconds, nameof(DeliveryRetryDelaySeconds), "RabbitMq delivery retry delay must be greater than zero");
        Positive(list, DeliveryMaxAttempts, nameof(DeliveryMaxAttempts), "RabbitMq delivery max attempts must be greater than zero");
        return list;
    }
    private static void Require(List<ValidationResult> list, string value, string name, string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }
    private static void Range(List<ValidationResult> list, int value, int low, int high, string name, string error)
    {
        if (value < low || value > high)
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }
    private static void Positive(List<ValidationResult> list, int value, string name, string error)
    {
        if (value <= 0)
        {
            list.Add(new ValidationResult(error, [name]));
        }
    }
    private static void Distinct(List<ValidationResult> list, string left, string right, string leftName, string rightName, string error)
    {
        if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.Ordinal))
        {
            list.Add(new ValidationResult(error, [leftName, rightName]));
        }
    }
}
