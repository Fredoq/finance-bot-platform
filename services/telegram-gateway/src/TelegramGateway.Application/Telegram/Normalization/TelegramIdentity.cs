using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Normalization;

/// <summary>
/// Represents the normalized Telegram user data required by the workspace slice.
/// Example:
/// <code>
/// var item = new TelegramIdentity(user);
/// </code>
/// </summary>
internal sealed record TelegramIdentity
{
    /// <summary>
    /// Initializes the normalized identity model from a Telegram user.
    /// Example:
    /// <code>
    /// var item = new TelegramIdentity(user);
    /// </code>
    /// </summary>
    /// <param name="user">The source Telegram user.</param>
    public TelegramIdentity(TelegramUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Id = user.Id;
        string first = user.FirstName?.Trim() ?? string.Empty;
        string last = user.LastName?.Trim() ?? string.Empty;
        string name = user.Username?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(last))
        {
            name = last;
        }
        if (!string.IsNullOrWhiteSpace(first))
        {
            name = first;
        }
        if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
        {
            name = $"{first} {last}";
        }
        Name = name;
        Locale = user.LanguageCode?.Trim() ?? string.Empty;
    }
    /// <summary>
    /// Gets the source user identifier.
    /// Example:
    /// <code>
    /// long id = item.Id;
    /// </code>
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the best-effort display name.
    /// Example:
    /// <code>
    /// string text = item.Name;
    /// </code>
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets the locale text.
    /// Example:
    /// <code>
    /// string text = item.Locale;
    /// </code>
    /// </summary>
    public string Locale { get; }
}
