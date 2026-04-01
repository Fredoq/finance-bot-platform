using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal interface IWorkspaceScreen
{
    TelegramText Message(long chatId, WorkspaceViewRequestedCommand command);
}
