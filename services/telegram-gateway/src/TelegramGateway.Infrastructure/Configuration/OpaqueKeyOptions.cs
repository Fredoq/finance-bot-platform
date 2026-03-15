using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the reversible opaque key secrets used by the gateway.
/// </summary>
public sealed class OpaqueKeyOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "Telegram:Keys";
    /// <summary>
    /// Gets or sets the current secret used to issue new keys.
    /// </summary>
    public string CurrentSecret { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the previous secrets still accepted for key rotation.
    /// </summary>
    public string[] PreviousSecrets { get; init; } = [];
    /// <summary>
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        var list = new List<ValidationResult>();
        if (string.IsNullOrWhiteSpace(CurrentSecret))
        {
            list.Add(new ValidationResult("Telegram key current secret is required", [nameof(CurrentSecret)]));
        }
        if (PreviousSecrets.Any(item => string.IsNullOrWhiteSpace(item)))
        {
            list.Add(new ValidationResult("Telegram key previous secrets must not contain blanks", [nameof(PreviousSecrets)]));
        }
        if (!string.IsNullOrWhiteSpace(CurrentSecret) && PreviousSecrets.Any(item => string.Equals(item, CurrentSecret, StringComparison.Ordinal)))
        {
            list.Add(new ValidationResult("Telegram key previous secrets must be distinct from the current secret", [nameof(CurrentSecret), nameof(PreviousSecrets)]));
        }
        return list;
    }
}
