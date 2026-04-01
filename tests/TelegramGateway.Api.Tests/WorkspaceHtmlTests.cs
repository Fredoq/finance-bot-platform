using TelegramGateway.Application.Entry.Workspace.Slices;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers workspace text formatting helpers.
/// </summary>
public sealed class WorkspaceHtmlTests
{
    /// <summary>
    /// Verifies that known currencies include a symbol.
    /// </summary>
    [Fact(DisplayName = "Formats known currency codes with symbols")]
    public void Formats_amount_with_symbol()
    {
        string text = new WorkspaceHtml().Amount(1200m, "USD");
        Assert.Equal("1 200 $ (<code>USD</code>)", text);
    }

    /// <summary>
    /// Verifies that known category codes include an icon.
    /// </summary>
    [Fact(DisplayName = "Formats category icons for known system codes")]
    public void Formats_category_with_icon()
    {
        string text = new WorkspaceHtml().Category("Food", "food");
        Assert.Equal("🍽 Food", text);
    }
}
