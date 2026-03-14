using System.ComponentModel.DataAnnotations;

namespace FinanceCore.Infrastructure.Configuration.Postgres;

/// <summary>
/// Represents the PostgreSQL settings required by the finance core runtime.
/// </summary>
public sealed class PostgresOptions : IValidatableObject
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string Section = "Postgres";
    /// <summary>
    /// Gets or sets the PostgreSQL connection string.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
    /// <summary>
    /// Validates the option values.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>The validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return [];
        }
        return [new ValidationResult("Postgres connection string is required", [nameof(ConnectionString)])];
    }
}
