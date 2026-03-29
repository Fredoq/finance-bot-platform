namespace FinanceCore.Domain.Workspace.Models;

/// <summary>
/// Describes the data required to resolve workspace actions for a state.
/// </summary>
public sealed record WorkspaceActionContext
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="homeAccountCount">The number of accounts visible on the home state.</param>
    /// <param name="accountChoiceCount">The number of account choices in the current state.</param>
    /// <param name="categoryChoiceCount">The number of category choices in the current state.</param>
    /// <param name="custom">Indicates whether custom input mode is enabled.</param>
    public WorkspaceActionContext(int homeAccountCount, int accountChoiceCount, int categoryChoiceCount, bool custom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(homeAccountCount);
        ArgumentOutOfRangeException.ThrowIfNegative(accountChoiceCount);
        ArgumentOutOfRangeException.ThrowIfNegative(categoryChoiceCount);
        HomeAccountCount = homeAccountCount;
        AccountChoiceCount = accountChoiceCount;
        CategoryChoiceCount = categoryChoiceCount;
        Custom = custom;
    }
    /// <summary>
    /// Gets the number of accounts visible on the home state.
    /// </summary>
    public int HomeAccountCount { get; }
    /// <summary>
    /// Gets the number of account choices in the current state.
    /// </summary>
    public int AccountChoiceCount { get; }
    /// <summary>
    /// Gets the number of category choices in the current state.
    /// </summary>
    public int CategoryChoiceCount { get; }
    /// <summary>
    /// Gets a value indicating whether custom input mode is enabled.
    /// </summary>
    public bool Custom { get; }
}
