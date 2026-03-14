namespace FinanceCore.Application.Runtime.Flow;

internal interface ICommandSlice
{
    string Contract { get; }
    ValueTask Run(ReadOnlyMemory<byte> body, CancellationToken token);
}
