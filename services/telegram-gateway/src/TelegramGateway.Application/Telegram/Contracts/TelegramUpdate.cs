using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram update payload consumed by the gateway anti-corruption layer.
/// Example:
/// <code>
/// var update = new TelegramUpdate { UpdateId = 1 };
/// </code>
/// </summary>
public sealed record TelegramUpdate
{
    /// <summary>
    /// Gets the source update identifier.
    /// Example:
    /// <code>
    /// long id = update.UpdateId;
    /// </code>
    /// </summary>
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }
    /// <summary>
    /// Gets the source message.
    /// Example:
    /// <code>
    /// TelegramMessage? item = update.Message;
    /// </code>
    /// </summary>
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }
}
