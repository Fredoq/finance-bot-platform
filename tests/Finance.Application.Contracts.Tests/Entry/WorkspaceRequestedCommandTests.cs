using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;

namespace Finance.Application.Contracts.Tests.Entry;

/// <summary>
/// Covers workspace requested command behavior.
/// Example:
/// <code>
/// var test = new WorkspaceRequestedCommandTests();
/// </code>
/// </summary>
public sealed class WorkspaceRequestedCommandTests
{
    private static readonly JsonSerializerOptions Note = new(JsonSerializerDefaults.Web);
    /// <summary>
    /// Verifies the public JSON shape of the command.
    /// Example:
    /// <code>
    /// await test.Serializes_command();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Serializes the workspace requested command with the expected fields")]
    public Task Serializes_command()
    {
        var item = new WorkspaceRequestedCommand("actor", "conversation", "Alex Doe", "en", "promo-42", DateTimeOffset.Parse("2026-03-11T09:00:00+00:00", CultureInfo.InvariantCulture));
        var note = JsonSerializer.Serialize(item, Note);
        Assert.Contains("\"actorKey\":\"actor\"", note, StringComparison.Ordinal);
        Assert.Contains("\"conversationKey\":\"conversation\"", note, StringComparison.Ordinal);
        Assert.Contains("\"payload\":\"promo-42\"", note, StringComparison.Ordinal);
        return Task.CompletedTask;
    }
}
