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
    /// Gets or sets the exchange name.
    /// </summary>
    public string Exchange { get; init; } = "finance.command";
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string Client { get; init; } = "telegram-gateway";
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
        if (string.IsNullOrWhiteSpace(Client))
        {
            list.Add(new ValidationResult("RabbitMq client is required", [nameof(Client)]));
        }
        return list;
    }
}
