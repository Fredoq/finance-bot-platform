using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram message payload consumed by the gateway anti-corruption layer.
/// Example:
/// <code>
/// var message = new TelegramMessage { Text = "/start" };
/// </code>
/// </summary>
public sealed record TelegramMessage
{
    /// <summary>
    /// Gets the message identifier.
    /// Example:
    /// <code>
    /// long id = message.MessageId;
    /// </code>
    /// </summary>
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }
    /// <summary>
    /// Gets the source actor data.
    /// Example:
    /// <code>
    /// TelegramUser? item = message.From;
    /// </code>
    /// </summary>
    [JsonPropertyName("from")]
    public TelegramUser? From { get; init; }
    /// <summary>
    /// Gets the source chat data.
    /// Example:
    /// <code>
    /// TelegramChat? item = message.Chat;
    /// </code>
    /// </summary>
    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; init; }
    /// <summary>
    /// Gets the Telegram message timestamp.
    /// Example:
    /// <code>
    /// long date = message.Date;
    /// </code>
    /// </summary>
    [JsonPropertyName("date")]
    public long Date { get; init; }
    /// <summary>
    /// Gets the message text.
    /// Example:
    /// <code>
    /// string? text = message.Text;
    /// </code>
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
    /// <summary>
    /// Gets the Telegram entities collection.
    /// Example:
    /// <code>
    /// TelegramEntity[]? items = message.Entities;
    /// </code>
    /// </summary>
    [JsonPropertyName("entities")]
    public TelegramEntity[]? Entities { get; init; }
}
