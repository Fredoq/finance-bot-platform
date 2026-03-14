using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram update payload consumed by the gateway anti-corruption layer.
/// </summary>
public sealed record TelegramUpdate
{
    /// <summary>
    /// Gets the source update identifier.
    /// </summary>
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }
    /// <summary>
    /// Gets the source message.
    /// </summary>
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }
}
