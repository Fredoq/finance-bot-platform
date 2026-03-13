using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents a Telegram message entity consumed by the gateway anti-corruption layer.
/// Example:
/// <code>
/// var entity = new TelegramEntity { Type = "bot_command", Offset = 0, Length = 6 };
/// </code>
/// </summary>
public sealed record TelegramEntity
{
    /// <summary>
    /// Gets the entity type.
    /// Example:
    /// <code>
    /// string? text = entity.Type;
    /// </code>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    /// <summary>
    /// Gets the entity offset.
    /// Example:
    /// <code>
    /// int number = entity.Offset;
    /// </code>
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; init; }
    /// <summary>
    /// Gets the entity length.
    /// Example:
    /// <code>
    /// int number = entity.Length;
    /// </code>
    /// </summary>
    [JsonPropertyName("length")]
    public int Length { get; init; }
}
