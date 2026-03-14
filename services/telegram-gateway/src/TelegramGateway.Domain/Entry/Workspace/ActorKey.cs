namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an opaque actor identifier for downstream contracts.
/// </summary>
/// <param name="Value">The opaque identifier value.</param>
public sealed record ActorKey(string Value)
{
    /// <summary>
    /// Gets the opaque identifier value.
    /// </summary>
    public string Value { get; } = string.IsNullOrWhiteSpace(Value) ? throw new ArgumentException("Actor key is required", nameof(Value)) : Value;
}
