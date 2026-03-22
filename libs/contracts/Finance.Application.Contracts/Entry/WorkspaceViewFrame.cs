namespace Finance.Application.Contracts.Entry;

/// <summary>
/// Represents the visible workspace frame that should be rendered.
/// </summary>
public sealed record WorkspaceViewFrame
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="state">The current workspace state code.</param>
    /// <param name="stateData">The serialized state data.</param>
    /// <param name="actions">The supported action codes.</param>
    public WorkspaceViewFrame(string state, string stateData, IReadOnlyList<string> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        string[] list = actions.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        State = !string.IsNullOrWhiteSpace(state) ? state.Trim() : throw new ArgumentException("Workspace state is required", nameof(state));
        StateData = stateData ?? throw new ArgumentNullException(nameof(stateData));
        Actions = list.Length > 0 ? Array.AsReadOnly(list) : throw new ArgumentException("Workspace actions are required", nameof(actions));
    }
    /// <summary>
    /// Gets the current workspace state code.
    /// </summary>
    public string State { get; }
    /// <summary>
    /// Gets the serialized state data.
    /// </summary>
    public string StateData { get; }
    /// <summary>
    /// Gets the supported action codes.
    /// </summary>
    public IReadOnlyList<string> Actions { get; }
}
