namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an opaque actor identifier for downstream contracts.
/// Example:
/// <code>
/// var key = new ActorKey("actor-7f4d");
/// </code>
/// </summary>
public sealed record ActorKey(string Value)
{
    /// <summary>
    /// Gets the opaque identifier value.
    /// Example:
    /// <code>
    /// var text = key.Value;
    /// </code>
    /// </summary>
    public string Value { get; } = string.IsNullOrWhiteSpace(Value) ? throw new ArgumentException("Actor key is required", nameof(Value)) : Value;
}
