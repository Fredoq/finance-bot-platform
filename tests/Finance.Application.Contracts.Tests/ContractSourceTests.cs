namespace Finance.Application.Contracts.Tests;

/// <summary>
/// Covers architecture guardrails for shared contracts.
/// Example:
/// <code>
/// var test = new ContractSourceTests();
/// </code>
/// </summary>
public sealed class ContractSourceTests
{
    /// <summary>
    /// Verifies that Telegram terms do not appear in shared contract source files.
    /// Example:
    /// <code>
    /// await test.Rejects_telegram();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Shared contract source files do not mention Telegram")]
    public Task Rejects_telegram()
    {
        var root = Root();
        var note = Path.Combine(root, "libs", "contracts", "Finance.Application.Contracts");
        var list = Directory.GetFiles(note, "*.cs", SearchOption.AllDirectories);
        foreach (var item in list)
        {
            var text = File.ReadAllText(item);
            Assert.DoesNotContain("Telegram", text, StringComparison.Ordinal);
        }
        return Task.CompletedTask;
    }
    /// <summary>
    /// Locates the repository root.
    /// Example:
    /// <code>
    /// string text = Root();
    /// </code>
    /// </summary>
    /// <returns>The repository root path.</returns>
    private static string Root()
    {
        DirectoryInfo? item = new(AppContext.BaseDirectory);
        while (item is not null && !File.Exists(Path.Combine(item.FullName, "finance-bot-platform.slnx")))
        {
            item = item.Parent;
        }
        return item?.FullName ?? throw new InvalidOperationException("Repository root was not found");
    }
}
