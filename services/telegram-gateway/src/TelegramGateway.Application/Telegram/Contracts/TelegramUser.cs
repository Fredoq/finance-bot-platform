using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram user payload consumed by the gateway anti-corruption layer.
/// </summary>
public sealed record TelegramUser
{
    /// <summary>
    /// Gets the source actor identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }
    /// <summary>
    /// Gets the first name text.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }
    /// <summary>
    /// Gets the last name text.
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }
    /// <summary>
    /// Gets the username text.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }
    /// <summary>
    /// Gets the locale text.
    /// </summary>
    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; init; }
}
