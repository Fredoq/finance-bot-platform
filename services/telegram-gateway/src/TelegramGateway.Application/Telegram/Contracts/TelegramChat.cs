using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram chat payload consumed by the gateway anti-corruption layer.
/// </summary>
public sealed record TelegramChat
{
    /// <summary>
    /// Gets the source conversation identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }
    /// <summary>
    /// Gets the chat type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
