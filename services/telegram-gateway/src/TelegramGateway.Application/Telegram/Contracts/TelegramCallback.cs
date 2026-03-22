using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram callback query payload consumed by the gateway anti-corruption layer.
/// </summary>
public sealed record TelegramCallback
{
    /// <summary>
    /// Gets the callback identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    /// <summary>
    /// Gets the source actor data.
    /// </summary>
    [JsonPropertyName("from")]
    public TelegramUser? From { get; init; }
    /// <summary>
    /// Gets the callback message.
    /// </summary>
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }
    /// <summary>
    /// Gets the callback data.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }
}
