using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the RabbitMQ settings required by the gateway transport.
/// Example:
/// <code>
/// var options = new RabbitMqOptions { Uri = "amqp://guest:guest@localhost:5672/", Exchange = "finance.command", Client = "telegram-gateway" };
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
    /// Gets or sets the broker URI.
    /// Example:
    /// <code>
    /// string text = options.Uri;
    /// </code>
    /// </summary>
    [Required]
    public string Uri { get; init; } = string.Empty;
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
        if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out var uri) || (uri.Scheme != "amqp" && uri.Scheme != "amqps"))
        {
            return [new ValidationResult("RabbitMq Uri must be an absolute amqp or amqps value", [nameof(Uri)])];
        }
        return [];
    }
}
