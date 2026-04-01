using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class WorkspaceScreen : IWorkspaceScreen
{
    private readonly WorkspaceBody body;
    private readonly WorkspaceText text;
    private readonly WorkspaceKeys rows;
    private readonly ITelegramKeys keys;

    public WorkspaceScreen(WorkspaceBody body, WorkspaceText text, WorkspaceKeys rows, ITelegramKeys keys)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        this.rows = rows ?? throw new ArgumentNullException(nameof(rows));
        this.keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }

    public TelegramText Message(long chatId, WorkspaceViewRequestedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        WorkspaceData data = body.Data(command.Frame.State, command.Frame.StateData);
        return new TelegramText(chatId, text.Text(command.Frame.State, command.Freshness.IsNewUser, data), rows.Rows(command.Frame.Actions, data), keys);
    }
}
