using System.Security.Cryptography;
using System.Text;

namespace TelegramGateway.Application.Keys;

internal sealed class OpaqueKey : IOpaqueKey
{
    /// <summary>
    /// Builds a deterministic opaque key.
    /// </summary>
    /// <param name="kind">The key kind.</param>
    /// <param name="scope">The key scope.</param>
    /// <param name="id">The identifier value.</param>
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
