using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace TelegramGateway.Application.Keys;

internal sealed class OpaqueKey : IOpaqueKey
{
    private readonly byte[][] list;
    public OpaqueKey(string current, IReadOnlyList<string> previous)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(current);
        ArgumentNullException.ThrowIfNull(previous);
        list = [.. new[] { current }.Concat(previous).Select(Bytes)];
    }
    public string Text(string kind, string scope, long id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        Span<byte> raw = stackalloc byte[8];
        Span<byte> mask = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(raw, id);
        Mask(list[0], kind, scope, mask);
        for (int item = 0; item < raw.Length; item++)
        {
            raw[item] ^= mask[item];
        }
        Span<byte> data = stackalloc byte[12];
        raw.CopyTo(data);
        Tag(list[0], kind, scope, id, data[8..12]);
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
        Span<byte> tag = stackalloc byte[4];
        foreach (byte[] item in list)
        {
            data.AsSpan(0, 8).CopyTo(raw);
            Mask(item, kind, scope, mask);
            for (int note = 0; note < raw.Length; note++)
            {
                raw[note] ^= mask[note];
            }
            long id = BinaryPrimitives.ReadInt64BigEndian(raw);
            Tag(item, kind, scope, id, tag);
            if (CryptographicOperations.FixedTimeEquals(tag, data.AsSpan(8, 4)))
            {
                return id;
            }
        }
        throw new ArgumentException("Opaque key signature is invalid", nameof(text));
    }
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
    private static void Mask(byte[] secret, string kind, string scope, Span<byte> target)
    {
        byte[] data = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes($"{kind}:{scope}:mask"));
        data.AsSpan(0, 8).CopyTo(target);
    }
    private static void Tag(byte[] secret, string kind, string scope, long id, Span<byte> target)
    {
        byte[] data = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes($"{kind}:{scope}:{id}:tag"));
        data.AsSpan(0, 4).CopyTo(target);
    }
}
