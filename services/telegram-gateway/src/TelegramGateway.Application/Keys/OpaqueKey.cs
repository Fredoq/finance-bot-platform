using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TelegramGateway.Application.Keys;

internal sealed class OpaqueKey : IOpaqueKey
{
    private const string Salt = "finance-bot-platform";
    public string Text(string kind, string scope, long id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        Span<byte> raw = stackalloc byte[8];
        Span<byte> mask = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(raw, id);
        Mask(kind, scope, mask);
        for (int item = 0; item < raw.Length; item++)
        {
            raw[item] ^= mask[item];
        }
        Span<byte> data = stackalloc byte[12];
        raw.CopyTo(data);
        Tag(kind, scope, id, data[8..12]);
        return $"{kind}-{Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }
    public long Id(string kind, string scope, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        string head = $"{kind}-";
        if (!text.StartsWith(head, StringComparison.Ordinal))
        {
            throw new ArgumentException("Opaque key kind is invalid", nameof(text));
        }
        string body = text[head.Length..].Replace('-', '+').Replace('_', '/');
        int tail = body.Length % 4;
        if (tail is > 0)
        {
            body = body.PadRight(body.Length + 4 - tail, '=');
        }
        byte[] data;
        try
        {
            data = Convert.FromBase64String(body);
        }
        catch (FormatException error)
        {
            throw new ArgumentException("Opaque key format is invalid", nameof(text), error);
        }
        if (data.Length != 12)
        {
            throw new ArgumentException("Opaque key length is invalid", nameof(text));
        }
        Span<byte> raw = stackalloc byte[8];
        Span<byte> mask = stackalloc byte[8];
        data.AsSpan(0, 8).CopyTo(raw);
        Mask(kind, scope, mask);
        for (int item = 0; item < raw.Length; item++)
        {
            raw[item] ^= mask[item];
        }
        long id = BinaryPrimitives.ReadInt64BigEndian(raw);
        Span<byte> tag = stackalloc byte[4];
        Tag(kind, scope, id, tag);
        if (!CryptographicOperations.FixedTimeEquals(tag, data.AsSpan(8, 4)))
        {
            throw new ArgumentException("Opaque key signature is invalid", nameof(text));
        }
        return id;
    }
    private static void Mask(string kind, string scope, Span<byte> target)
    {
        byte[] data = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{Salt}:{kind}:{scope}:mask"));
        data.AsSpan(0, 8).CopyTo(target);
    }
    private static void Tag(string kind, string scope, long id, Span<byte> target)
    {
        byte[] data = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{Salt}:{kind}:{scope}:{id}"));
        data.AsSpan(0, 4).CopyTo(target);
    }
}
