namespace FinanceCore.Domain.Workspace.Models;

/// <summary>
/// Represents the current workspace state snapshot.
/// </summary>
public sealed record WorkspaceState
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="code">The state code.</param>
    /// <param name="data">The serialized state data.</param>
    /// <param name="revision">The state revision.</param>
    public WorkspaceState(string code, string data, long revision)
    {
        Code = !string.IsNullOrWhiteSpace(code) ? code : throw new ArgumentException("Workspace state code is required", nameof(code));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Revision = revision > 0 ? revision : throw new ArgumentOutOfRangeException(nameof(revision));
    }
    /// <summary>
    /// Gets the state code.
    /// </summary>
    public string Code { get; }
    /// <summary>
    /// Gets the serialized state data.
    /// </summary>
    public string Data { get; }
    /// <summary>
    /// Gets the state revision.
    /// </summary>
    public long Revision { get; }
}
