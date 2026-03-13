namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an opaque conversation identifier for downstream contracts.
/// Example:
/// <code>
/// var key = new ConversationKey("conversation-7f4d");
/// </code>
/// </summary>
public sealed record ConversationKey(string Value)
{
    /// <summary>
    /// Gets the opaque identifier value.
    /// Example:
    /// <code>
    /// var text = key.Value;
    /// </code>
    /// </summary>
    public string Value { get; } = string.IsNullOrWhiteSpace(Value) ? throw new ArgumentException("Conversation key is required", nameof(Value)) : Value;
}
