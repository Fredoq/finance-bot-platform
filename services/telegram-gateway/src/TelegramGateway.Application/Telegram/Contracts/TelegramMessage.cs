using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram message payload consumed by the gateway anti-corruption layer.
/// </summary>
public sealed record TelegramMessage
{
    /// <summary>
    /// Gets the message identifier.
    /// </summary>
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }
    /// <summary>
    /// Gets the source actor data.
    /// </summary>
    [JsonPropertyName("from")]
    public TelegramUser? From { get; init; }
    /// <summary>
    /// Gets the source chat data.
    /// </summary>
    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; init; }
    /// <summary>
    /// Gets the Telegram message timestamp.
    /// </summary>
    [JsonPropertyName("date")]
    public long Date { get; init; }
    /// <summary>
    /// Gets the message text.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
    /// <summary>
    /// Gets the Telegram entities collection.
    /// </summary>
    [JsonPropertyName("entities")]
    public TelegramEntity[]? Entities { get; init; }
}
