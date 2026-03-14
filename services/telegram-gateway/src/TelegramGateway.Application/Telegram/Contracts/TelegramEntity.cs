using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents a Telegram message entity consumed by the gateway anti-corruption layer.
/// </summary>
public sealed record TelegramEntity
{
    /// <summary>
    /// Gets the entity type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    /// <summary>
    /// Gets the entity offset.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; init; }
    /// <summary>
    /// Gets the entity length.
    /// </summary>
    [JsonPropertyName("length")]
    public int Length { get; init; }
}
