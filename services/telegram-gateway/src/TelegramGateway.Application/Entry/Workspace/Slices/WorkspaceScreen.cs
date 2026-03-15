using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace;

internal static class WorkspaceScreen
{
    public static TelegramText Message(long chatId, WorkspaceViewRequestedCommand command) => new(chatId, Text(command), Keys(command));
    private static string Text(WorkspaceViewRequestedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string head = command.IsNewUser ? "Welcome to your finance workspace" : "Your finance workspace is ready";
        string body = command.IsNewWorkspace ? "A new conversation was opened" : "Your current conversation was restored";
        return $"{head}\nState: {command.State}\n{body}\nChoose the next action";
    }
    private static IReadOnlyList<TelegramRow> Keys(WorkspaceViewRequestedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TelegramButton[] item = [.. command.Actions.Select(code => Button(code))];
        return [.. item.Chunk(2).Select(item => new TelegramRow([.. item]))];
    }
    private static TelegramButton Button(string code) => code switch
    {
        "transaction.expense.add" => new TelegramButton("Add expense", code),
        "transaction.income.add" => new TelegramButton("Add income", code),
        "summary.month.show" => new TelegramButton("Show month", code),
        "category.breakdown.show" => new TelegramButton("Show categories", code),
        "transaction.recent.show" => new TelegramButton("Show recent", code),
        _ => new TelegramButton(code, code)
    };
}
