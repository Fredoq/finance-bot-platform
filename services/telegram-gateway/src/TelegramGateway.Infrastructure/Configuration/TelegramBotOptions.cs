using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the Telegram Bot API settings required by the gateway runtime.
/// </summary>
public sealed class TelegramBotOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "Telegram:Bot";
    /// <summary>
    /// Gets or sets the bot token.
    /// </summary>
    public string Token { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the Bot API base address.
    /// </summary>
    public string BaseUrl { get; init; } = "https://api.telegram.org";
    /// <summary>
    /// Gets or sets the HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 10;
    /// <summary>
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        var list = new List<ValidationResult>();
        if (string.IsNullOrWhiteSpace(Token))
        {
            list.Add(new ValidationResult("Telegram bot token is required", [nameof(Token)]));
        }
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? item) || item.Scheme is not ("http" or "https"))
        {
            list.Add(new ValidationResult("Telegram bot base url must be absolute", [nameof(BaseUrl)]));
        }
        if (TimeoutSeconds <= 0)
        {
            list.Add(new ValidationResult("Telegram bot timeout must be greater than zero", [nameof(TimeoutSeconds)]));
        }
        return list;
    }
}
