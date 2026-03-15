using TelegramGateway.Application.Keys;

namespace TelegramGateway.Api.Tests;

/// <summary>
/// Covers reversible opaque key behavior.
/// </summary>
public sealed class OpaqueKeyTests
{
    /// <summary>
    /// Verifies that a conversation key can be decoded after encoding.
    /// </summary>
    [Fact(DisplayName = "Decodes the original identifier from an opaque conversation key")]
    public void Restores_identifier()
    {
        var item = new OpaqueKey();
        string text = item.Text("conversation", "telegram:chat", 42);
        long data = item.Id("conversation", "telegram:chat", text);
        Assert.Equal(42, data);
    }
    /// <summary>
    /// Verifies that tampered opaque keys are rejected.
    /// </summary>
    [Fact(DisplayName = "Rejects a tampered opaque key")]
    public void Rejects_tamper()
    {
        var item = new OpaqueKey();
        string text = item.Text("conversation", "telegram:chat", 42);
        string data = $"{text[..^1]}A";
        Assert.Throws<ArgumentException>(() => item.Id("conversation", "telegram:chat", data));
    }
}
