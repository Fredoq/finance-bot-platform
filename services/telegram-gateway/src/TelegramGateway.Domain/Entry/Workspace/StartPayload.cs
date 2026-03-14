namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an optional payload that follows the entry command.
/// </summary>
/// <param name="Value">The payload value.</param>
public sealed record StartPayload(string Value)
{
    /// <summary>
    /// Gets the payload value.
    /// </summary>
    public string Value { get; } = Value ?? throw new ArgumentNullException(nameof(Value));
}
