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
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var list = new List<ValidationResult>();
        if (string.IsNullOrWhiteSpace(Host))
        {
            list.Add(new ValidationResult("RabbitMq host is required", [nameof(Host)]));
        }
        if (Port is < 1 or > 65535)
        {
            list.Add(new ValidationResult("RabbitMq port must be between 1 and 65535", [nameof(Port)]));
        }
        if (string.IsNullOrWhiteSpace(VirtualHost))
        {
            list.Add(new ValidationResult("RabbitMq virtual host is required", [nameof(VirtualHost)]));
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            list.Add(new ValidationResult("RabbitMq username is required", [nameof(Username)]));
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            list.Add(new ValidationResult("RabbitMq password is required", [nameof(Password)]));
        }
        if (string.IsNullOrWhiteSpace(Exchange))
        {
            list.Add(new ValidationResult("RabbitMq exchange is required", [nameof(Exchange)]));
        }
        if (string.IsNullOrWhiteSpace(Queue))
        {
            list.Add(new ValidationResult("RabbitMq queue is required", [nameof(Queue)]));
        }
        if (string.IsNullOrWhiteSpace(RetryQueue))
        {
            list.Add(new ValidationResult("RabbitMq retry queue is required", [nameof(RetryQueue)]));
        }
        if (string.IsNullOrWhiteSpace(DeadQueue))
        {
            list.Add(new ValidationResult("RabbitMq dead queue is required", [nameof(DeadQueue)]));
        }
        if (string.IsNullOrWhiteSpace(Client))
        {
            list.Add(new ValidationResult("RabbitMq client is required", [nameof(Client)]));
        }
        if (Prefetch == 0)
        {
            list.Add(new ValidationResult("RabbitMq prefetch must be greater than zero", [nameof(Prefetch)]));
        }
        if (RetryDelaySeconds <= 0)
        {
            list.Add(new ValidationResult("RabbitMq retry delay must be greater than zero", [nameof(RetryDelaySeconds)]));
        }
        if (MaxAttempts <= 0)
        {
            list.Add(new ValidationResult("RabbitMq max attempts must be greater than zero", [nameof(MaxAttempts)]));
        }
        return list;
    }
}
