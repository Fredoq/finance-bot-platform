namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an opaque conversation identifier for downstream contracts.
/// </summary>
public sealed record ConversationKey
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="value">The opaque identifier value.</param>
    public ConversationKey(string value) => Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Conversation key is required", nameof(value)) : value;
    /// <summary>
    /// Gets the opaque identifier value.
    /// </summary>
    public string Value { get; }
}
