using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the RabbitMQ settings required by the gateway transport.
/// Example:
/// <code>
/// var options = new RabbitMqOptions { Host = "localhost", Port = 5672, VirtualHost = "/", Username = "guest", Password = "guest", Exchange = "finance.command", Client = "telegram-gateway" };
/// </code>
/// </summary>
public sealed class RabbitMqOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// Example:
    /// <code>
    /// string text = RabbitMqOptions.Section;
    /// </code>
    /// </summary>
    public const string Section = "RabbitMq";
    /// <summary>
    /// Gets or sets the broker host name.
    /// Example:
    /// <code>
    /// string text = options.Host;
    /// </code>
    /// </summary>
    [Required]
    public string Host { get; init; } = "localhost";
    /// <summary>
    /// Gets or sets the broker port.
    /// Example:
    /// <code>
    /// int item = options.Port;
    /// </code>
    /// </summary>
    public int Port { get; init; } = 5672;
    /// <summary>
    /// Gets or sets the broker virtual host.
    /// Example:
    /// <code>
    /// string text = options.VirtualHost;
    /// </code>
    /// </summary>
    [Required]
    public string VirtualHost { get; init; } = "/";
    /// <summary>
    /// Gets or sets the broker user name.
    /// Example:
    /// <code>
    /// string text = options.Username;
    /// </code>
    /// </summary>
    [Required]
    public string Username { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the broker password.
    /// Example:
    /// <code>
    /// string text = options.Password;
    /// </code>
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the exchange name.
    /// Example:
    /// <code>
    /// string text = options.Exchange;
    /// </code>
    /// </summary>
    [Required]
    public string Exchange { get; init; } = "finance.command";
    /// <summary>
    /// Gets or sets the client name.
    /// Example:
    /// <code>
    /// string text = options.Client;
    /// </code>
    /// </summary>
    [Required]
    public string Client { get; init; } = "telegram-gateway";
    /// <summary>
    /// Validates the option values.
    /// Example:
    /// <code>
    /// IEnumerable&lt;ValidationResult&gt; items = options.Validate(new ValidationContext(options));
    /// </code>
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation result collection.</returns>
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
        return list;
    }
}
