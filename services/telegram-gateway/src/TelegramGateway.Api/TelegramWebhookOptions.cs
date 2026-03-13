using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Api;

/// <summary>
/// Represents the Telegram webhook settings used by the gateway edge.
/// Example:
/// <code>
/// var options = new TelegramWebhookOptions { SecretToken = "secret" };
/// </code>
/// </summary>
public sealed class TelegramWebhookOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// Example:
    /// <code>
    /// string text = TelegramWebhookOptions.Section;
    /// </code>
    /// </summary>
    public const string Section = "Telegram:Webhook";
    /// <summary>
    /// Gets or sets the expected webhook secret token.
    /// Example:
    /// <code>
    /// string text = options.SecretToken;
    /// </code>
    /// </summary>
    [Required]
    public string SecretToken { get; init; } = string.Empty;
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
        if (string.IsNullOrWhiteSpace(SecretToken))
        {
            return [new ValidationResult("Telegram webhook secret token is required", [nameof(SecretToken)])];
        }
        return [];
    }
}
