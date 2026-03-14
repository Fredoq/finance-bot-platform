using FinanceCore.Application.Runtime.Faults;

namespace FinanceCore.Application.Runtime.Flow;

internal sealed class CommandFlow : ICommandFlow
{
    private readonly IReadOnlyDictionary<string, ICommandSlice> map;
    internal CommandFlow(IEnumerable<ICommandSlice> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        map = list.ToDictionary(item => item.Contract, StringComparer.Ordinal);
    }
    public ValueTask Run(string contract, ReadOnlyMemory<byte> body, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(contract))
        {
            throw new InvalidMessageException("Message contract is required");
        }
        if (!map.TryGetValue(contract, out ICommandSlice? item))
        {
            throw new InvalidMessageException("Message contract is unsupported");
        }
        return item.Run(body, token);
    }
}
