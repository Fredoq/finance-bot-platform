namespace TelegramGateway.Application.Keys;

/// <summary>
/// Describes the opaque key policy used for downstream identity mapping.
/// Example:
/// <code>
/// string text = item.Text("actor", "telegram:user", 42);
/// </code>
/// </summary>
internal interface IOpaqueKey
{
    /// <summary>
    /// Builds a deterministic opaque key.
    /// Example:
    /// <code>
    /// string text = item.Text("actor", "telegram:user", 42);
    /// </code>
    /// </summary>
    /// <param name="kind">The downstream key kind.</param>
    /// <param name="scope">The source namespace.</param>
    /// <param name="id">The source identifier.</param>
    /// <returns>The opaque key text.</returns>
    string Text(string kind, string scope, long id);
}
