namespace TelegramGateway.Domain.Entry.Workspace;

/// <summary>
/// Represents an optional payload that follows the entry command.
/// </summary>
public sealed record StartPayload
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="value">The payload value.</param>
    public StartPayload(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
    /// <summary>
    /// Gets the payload value.
    /// </summary>
    public string Value { get; }
}
