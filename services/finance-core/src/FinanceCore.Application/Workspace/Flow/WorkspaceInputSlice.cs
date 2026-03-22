using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Runtime.Faults;
using FinanceCore.Application.Runtime.Flow;
using FinanceCore.Application.Workspace.Ports;

namespace FinanceCore.Application.Workspace.Flow;

internal sealed class WorkspaceInputSlice : ICommandSlice
{
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly IWorkspaceInputPort port;
    internal WorkspaceInputSlice(IWorkspaceInputPort port) => this.port = port ?? throw new ArgumentNullException(nameof(port));
    public string Contract => "workspace.input.requested";
    public async ValueTask Run(ReadOnlyMemory<byte> body, CancellationToken token)
    {
        MessageEnvelope<WorkspaceInputRequestedCommand>? item;
        try
        {
            item = JsonSerializer.Deserialize<MessageEnvelope<WorkspaceInputRequestedCommand>>(body.Span, json);
        }
        catch (Exception error) when (error is JsonException or NotSupportedException)
        {
            throw new InvalidMessageException("Message payload is invalid", error);
        }
        if (item is null)
        {
            throw new InvalidMessageException("Message payload is invalid");
        }
        await port.Save(item, token);
    }
}
