namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an optional payload that follows the entry command.
/// Example:
/// <code>
/// var payload = new StartPayload("campaign-42");
/// </code>
/// </summary>
public sealed record StartPayload(string Value)
{
    /// <summary>
    /// Gets the payload value.
    /// Example:
    /// <code>
    /// var text = payload.Value;
    /// </code>
    /// </summary>
    public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
}
