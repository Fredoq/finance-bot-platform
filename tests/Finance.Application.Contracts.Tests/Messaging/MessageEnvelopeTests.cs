using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;

namespace Finance.Application.Contracts.Tests.Messaging;

/// <summary>
/// Covers message envelope serialization behavior.
/// Example:
/// <code>
/// var test = new MessageEnvelopeTests();
/// </code>
/// </summary>
public sealed class MessageEnvelopeTests
{
    private static readonly JsonSerializerOptions Note = new(JsonSerializerDefaults.Web);
    /// <summary>
    /// Verifies the public JSON shape of the envelope.
    /// Example:
    /// <code>
    /// await test.Serializes_envelope();
    /// </code>
    /// </summary>
    [Fact(DisplayName = "Serializes the message envelope with the expected wire shape")]
    public Task Serializes_envelope()
    {
        var item = new MessageEnvelope<WorkspaceRequestedCommand>(Guid.Parse("018f65a5-59f5-7b22-8f5e-c8ac0fe6bc90"), "workspace.requested", DateTimeOffset.Parse("2026-03-11T09:00:00+00:00", CultureInfo.InvariantCulture), "trace-1", "edge-update-1", "edge-update-1", "telegram-gateway", new WorkspaceRequestedCommand("actor", "conversation", "Alex Doe", "en", "promo-42", DateTimeOffset.Parse("2026-03-11T09:00:00+00:00", CultureInfo.InvariantCulture)));
        var note = JsonSerializer.Serialize(item, Note);
        Assert.Contains("\"messageId\"", note, StringComparison.Ordinal);
        Assert.Contains("\"contract\":\"workspace.requested\"", note, StringComparison.Ordinal);
        Assert.DoesNotContain("\"version\"", note, StringComparison.Ordinal);
        Assert.Contains("\"payload\"", note, StringComparison.Ordinal);
        return Task.CompletedTask;
    }
}
