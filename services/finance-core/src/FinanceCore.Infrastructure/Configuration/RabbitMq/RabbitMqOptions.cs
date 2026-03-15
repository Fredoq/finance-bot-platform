using System.ComponentModel.DataAnnotations;

namespace FinanceCore.Infrastructure.Configuration.RabbitMq;

/// <summary>
/// Represents the RabbitMQ settings required by the finance core runtime.
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
    /// Gets or sets the public command exchange name.
    /// </summary>
    public string Exchange { get; init; } = "finance.command";
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
    public string Client { get; init; } = "finance-core";
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
        Require(list, Host, nameof(Host), "RabbitMq host is required");
        Range(list, Port, 1, 65535, nameof(Port), "RabbitMq port must be between 1 and 65535");
        Require(list, VirtualHost, nameof(VirtualHost), "RabbitMq virtual host is required");
        Require(list, Username, nameof(Username), "RabbitMq username is required");
        Require(list, Password, nameof(Password), "RabbitMq password is required");
        Require(list, Exchange, nameof(Exchange), "RabbitMq exchange is required");
        Require(list, Queue, nameof(Queue), "RabbitMq queue is required");
        Require(list, RetryQueue, nameof(RetryQueue), "RabbitMq retry queue is required");
        Require(list, DeadQueue, nameof(DeadQueue), "RabbitMq dead queue is required");
        Distinct(list, Queue, RetryQueue, nameof(Queue), nameof(RetryQueue), "RabbitMq queue and retry queue must be distinct");
        Distinct(list, Queue, DeadQueue, nameof(Queue), nameof(DeadQueue), "RabbitMq queue and dead queue must be distinct");
        Distinct(list, RetryQueue, DeadQueue, nameof(RetryQueue), nameof(DeadQueue), "RabbitMq retry queue and dead queue must be distinct");
        Require(list, Client, nameof(Client), "RabbitMq client is required");
        Positive(list, Prefetch, nameof(Prefetch), "RabbitMq prefetch must be greater than zero");
        Positive(list, RetryDelaySeconds, nameof(RetryDelaySeconds), "RabbitMq retry delay must be greater than zero");
        Positive(list, MaxAttempts, nameof(MaxAttempts), "RabbitMq max attempts must be greater than zero");
        Positive(list, OutboxBatchSize, nameof(OutboxBatchSize), "RabbitMq outbox batch size must be greater than zero");
        return list;
    }
    private static void Require(List<ValidationResult> list, string value, string name, string error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            list.Add(Result(error, name));
        }
    }
    private static void Range(List<ValidationResult> list, int value, int low, int high, string name, string error)
    {
        if (value < low || value > high)
        {
            list.Add(Result(error, name));
        }
    }
    private static void Positive(List<ValidationResult> list, int value, string name, string error)
    {
        if (value <= 0)
        {
            list.Add(Result(error, name));
        }
    }
    private static void Distinct(List<ValidationResult> list, string left, string right, string leftName, string rightName, string error)
    {
        if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.Ordinal))
        {
            list.Add(new ValidationResult(error, [leftName, rightName]));
        }
    }
    private static ValidationResult Result(string error, string name) => new(error, [name]);
}
