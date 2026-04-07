using System.ComponentModel.DataAnnotations;

namespace TelegramGateway.Infrastructure.Configuration;

/// <summary>
/// Represents the Redis settings used by the gateway transport context cache.
/// </summary>
public sealed class RedisOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "Redis";
    /// <summary>
    /// Gets or sets the Redis connection string.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the optional key prefix.
    /// </summary>
    public string KeyPrefix { get; init; } = string.Empty;
    /// <summary>
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return [new ValidationResult("Redis connection string is required", [nameof(ConnectionString)])];
        }
        return [];
    }
}
