using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Api;

/// <summary>
/// Represents the Telegram webhook settings used by the gateway edge.
/// </summary>
public sealed class TelegramWebhookOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "Telegram:Webhook";
    /// <summary>
    /// Gets or sets the expected webhook secret token.
    /// </summary>
    public string SecretToken { get; init; } = string.Empty;
    /// <summary>
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SecretToken))
        {
            return [new ValidationResult("Telegram webhook secret token is required", [nameof(SecretToken)])];
        }
        return [];
    }
}
