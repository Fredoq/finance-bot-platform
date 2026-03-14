using System.Security.Cryptography;
using System.Text;

namespace TelegramGateway.Application.Keys;

/// <summary>
/// Builds deterministic opaque keys for external identifiers.
/// Example:
/// <code>
/// string text = item.Text("actor", "telegram:user", 42);
/// </code>
/// </summary>
internal sealed class OpaqueKey : IOpaqueKey
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
    public string Text(string kind, string scope, long id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        byte[] data = Encoding.UTF8.GetBytes($"{scope}:{id}");
        byte[] hash = SHA256.HashData(data);
        return $"{kind}-{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
