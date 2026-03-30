namespace FinanceCore.Domain.Workspace.Models;

/// <summary>
/// Describes paging data for recent transaction actions.
/// </summary>
public sealed record RecentPaging
{
    /// <summary>
    /// Initializes a new instance of the record.
    /// </summary>
    /// <param name="hasPrevious">Indicates whether the current recent page has a previous page.</param>
    /// <param name="hasNext">Indicates whether the current recent page has a next page.</param>
    public RecentPaging(bool hasPrevious, bool hasNext)
    {
        HasPrevious = hasPrevious;
        HasNext = hasNext;
    }
    /// <summary>
    /// Gets a value indicating whether the current recent page has a previous page.
    /// </summary>
    public bool HasPrevious { get; }
    /// <summary>
    /// Gets a value indicating whether the current recent page has a next page.
    /// </summary>
    public bool HasNext { get; }
}

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
    /// <param name="recentItemCount">The number of recent transaction items in the current state.</param>
    /// <param name="recent">The paging state for the current recent transactions page.</param>
    /// <param name="custom">Indicates whether custom input mode is enabled.</param>
    public WorkspaceActionContext(int homeAccountCount, int accountChoiceCount, int categoryChoiceCount, int recentItemCount, RecentPaging recent, bool custom)
    {
        ArgumentNullException.ThrowIfNull(recent);
        ArgumentOutOfRangeException.ThrowIfNegative(homeAccountCount);
        ArgumentOutOfRangeException.ThrowIfNegative(accountChoiceCount);
        ArgumentOutOfRangeException.ThrowIfNegative(categoryChoiceCount);
        ArgumentOutOfRangeException.ThrowIfNegative(recentItemCount);
        HomeAccountCount = homeAccountCount;
        AccountChoiceCount = accountChoiceCount;
        CategoryChoiceCount = categoryChoiceCount;
        RecentItemCount = recentItemCount;
        Recent = recent;
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
    /// Gets the number of recent transaction items in the current state.
    /// </summary>
    public int RecentItemCount { get; }
    /// <summary>
    /// Gets the paging state for recent transaction actions.
    /// </summary>
    public RecentPaging Recent { get; }
    /// <summary>
    /// Gets a value indicating whether the current recent page has a previous page.
    /// </summary>
    public bool HasPrevious => Recent.HasPrevious;
    /// <summary>
    /// Gets a value indicating whether the current recent page has a next page.
    /// </summary>
    public bool HasNext => Recent.HasNext;
    /// <summary>
    /// Gets a value indicating whether custom input mode is enabled.
    /// </summary>
    public bool Custom { get; }
}
