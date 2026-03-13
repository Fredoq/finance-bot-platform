using System.Text.Json.Serialization;

namespace TelegramGateway.Application.Telegram.Contracts;

/// <summary>
/// Represents the Telegram user payload consumed by the gateway anti-corruption layer.
/// Example:
/// <code>
/// var user = new TelegramUser { Id = 1, FirstName = "Alex" };
/// </code>
/// </summary>
public sealed record TelegramUser
{
    /// <summary>
    /// Gets the source actor identifier.
    /// Example:
    /// <code>
    /// long id = user.Id;
    /// </code>
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }
    /// <summary>
    /// Gets the first name text.
    /// Example:
    /// <code>
    /// string? text = user.FirstName;
    /// </code>
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }
    /// <summary>
    /// Gets the last name text.
    /// Example:
    /// <code>
    /// string? text = user.LastName;
    /// </code>
    /// </summary>
    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }
    /// <summary>
    /// Gets the username text.
    /// Example:
    /// <code>
    /// string? text = user.Username;
    /// </code>
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }
    /// <summary>
    /// Gets the locale text.
    /// Example:
    /// <code>
    /// string? text = user.LanguageCode;
    /// </code>
    /// </summary>
    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; init; }
}
