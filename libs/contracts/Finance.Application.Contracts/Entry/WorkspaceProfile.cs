namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents the immutable user profile details attached to a workspace request.
/// </summary>
/// <param name="Name">The display name.</param>
/// <param name="Locale">The locale code.</param>
public sealed record WorkspaceProfile(
    string Name,
    string Locale)
{
    /// <summary>
    /// Gets the display name associated with the workspace request.
    /// </summary>
    public string Name { get; init; } = Name ?? throw new ArgumentNullException(nameof(Name));
    /// <summary>
    /// Gets the locale associated with the workspace request.
    /// </summary>
    public string Locale { get; init; } = Locale ?? throw new ArgumentNullException(nameof(Locale));
}
