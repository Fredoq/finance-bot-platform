using TelegramGateway.Application.Telegram.Contracts;

namespace TelegramGateway.Application.Telegram.Normalization;

internal sealed record TelegramIdentity
{
    /// <summary>
    /// Normalizes a Telegram user into gateway identity fields.
    /// </summary>
    /// <param name="user">The Telegram user.</param>
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
    /// Gets the Telegram user identifier.
    /// </summary>
    public long Id { get; }
    /// <summary>
    /// Gets the normalized display name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets the normalized locale code.
    /// </summary>
    public string Locale { get; }
}
