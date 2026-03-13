using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram chat payload consumed by the gateway anti-corruption layer.
/// Example:
/// <code>
/// var chat = new TelegramChat { Id = 1, Type = "private" };
/// </code>
/// </summary>
public sealed record TelegramChat
{
    /// <summary>
    /// Gets the source conversation identifier.
    /// Example:
    /// <code>
    /// long id = chat.Id;
    /// </code>
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }
    /// <summary>
    /// Gets the chat type.
    /// Example:
    /// <code>
    /// string? text = chat.Type;
    /// </code>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}
