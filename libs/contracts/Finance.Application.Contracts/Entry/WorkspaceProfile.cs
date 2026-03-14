namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents the immutable user profile details attached to a workspace request.
/// </summary>
public sealed record WorkspaceProfile
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <param name="locale">The locale code.</param>
    public WorkspaceProfile(string name, string locale)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Locale = locale ?? throw new ArgumentNullException(nameof(locale));
    }
    /// <summary>
    /// Gets the display name associated with the workspace request.
    /// </summary>
    public string Name { get; init; }
    /// <summary>
    /// Gets the locale associated with the workspace request.
    /// </summary>
    public string Locale { get; init; }
}
