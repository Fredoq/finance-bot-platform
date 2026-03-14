namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an opaque actor identifier for downstream contracts.
/// </summary>
public sealed record ActorKey
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="value">The opaque identifier value.</param>
    public ActorKey(string value) => Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Actor key is required", nameof(value)) : value;
    /// <summary>
    /// Gets the opaque identifier value.
    /// </summary>
    public string Value { get; }
}
