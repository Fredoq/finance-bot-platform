using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class WorkspaceKeys
{
    private const string ExpenseAccountSlot = "transaction.expense.account.";
    private const string ExpenseCategorySlot = "transaction.expense.category.";
    private const string IncomeAccountSlot = "transaction.income.account.";
    private const string IncomeCategorySlot = "transaction.income.category.";
    private const string TransferSourceSlot = "transfer.source.account.";
    private const string TransferTargetSlot = "transfer.target.account.";
    private const string RecentItemSlot = "transaction.recent.item.";
    private const string RecentCategorySlot = "transaction.recent.category.";
    private const string Primary = "primary";
    private const string Success = "success";
    private const string Danger = "danger";
    private const string Cancel = "✖ Cancel";
    private readonly WorkspaceHtml html;

    public WorkspaceKeys(WorkspaceHtml html) => this.html = html ?? throw new ArgumentNullException(nameof(html));

    internal IReadOnlyList<TelegramRow> Rows(IReadOnlyList<string> actions, WorkspaceData data)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(data);
        TelegramButton[] item = [.. actions.Select(code => Button(code, data))];
        return [.. item.Chunk(2).Select(row => new TelegramRow([.. row]))];
    }

    private TelegramButton Button(string code, WorkspaceData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return code switch
        {
            "account.add" => new TelegramButton("➕ Add account", code, Primary),
            "account.currency.rub" => new TelegramButton("RUB ₽", code),
            "account.currency.usd" => new TelegramButton("USD $", code),
            "account.currency.eur" => new TelegramButton("EUR €", code),
            "account.currency.other" => new TelegramButton("Other", code),
            "account.create" => new TelegramButton("✅ Create account", code, Success),
            "account.cancel" => new TelegramButton(Cancel, code, Danger),
            "transaction.expense.add" => new TelegramButton("➖ Add expense", code, Primary),
            "transaction.income.add" => new TelegramButton("➕ Add income", code, Primary),
            "transfer.add" => new TelegramButton("↔ Transfer", code, Primary),
            "transaction.expense.create" => new TelegramButton("✅ Save expense", code, Success),
            "transaction.expense.cancel" => new TelegramButton(Cancel, code, Danger),
            "transaction.income.create" => new TelegramButton("✅ Save income", code, Success),
            "transaction.income.cancel" => new TelegramButton(Cancel, code, Danger),
            "transfer.create" => new TelegramButton("✅ Save transfer", code, Success),
            "transfer.cancel" => new TelegramButton(Cancel, code, Danger),
            "transaction.recent.show" => new TelegramButton("🧾 Recent transactions", code, Primary),
            "summary.month.show" => new TelegramButton("📊 Monthly summary", code, Primary),
            "category.month.show" => new TelegramButton("🗂 Category breakdown", code, Primary),
            "profile.timezone.show" => new TelegramButton("🕒 Time zone", code, Primary),
            "summary.month.prev" => new TelegramButton("◀ Previous month", code),
            "summary.month.next" => new TelegramButton("Next month ▶", code),
            "summary.month.back" => new TelegramButton("↩ Back", code),
            "category.month.prev" => new TelegramButton("◀ Previous month", code),
            "category.month.next" => new TelegramButton("Next month ▶", code),
            "category.month.back" => new TelegramButton("↩ Back", code),
            "profile.timezone.cancel" => new TelegramButton(Cancel, code, Danger),
            "transaction.recent.page.prev" => new TelegramButton("◀ Previous", code),
            "transaction.recent.page.next" => new TelegramButton("Next ▶", code),
            "transaction.recent.back" => new TelegramButton("↩ Back", code),
            "transaction.recent.delete" => new TelegramButton("🗑 Delete", code, Danger),
            "transaction.recent.delete.apply" => new TelegramButton("✅ Delete transaction", code, Danger),
            "transaction.recent.recategorize" => new TelegramButton("✏ Change category", code, Primary),
            "transaction.recent.recategorize.apply" => new TelegramButton("✅ Save category", code, Success),
            _ when code.StartsWith(ExpenseAccountSlot, StringComparison.Ordinal) => Account(code, data, ExpenseAccountSlot),
            _ when code.StartsWith(IncomeAccountSlot, StringComparison.Ordinal) => Account(code, data, IncomeAccountSlot),
            _ when code.StartsWith(TransferSourceSlot, StringComparison.Ordinal) => Account(code, data, TransferSourceSlot),
            _ when code.StartsWith(TransferTargetSlot, StringComparison.Ordinal) => Account(code, data, TransferTargetSlot),
            _ when code.StartsWith(ExpenseCategorySlot, StringComparison.Ordinal) => Category(code, data, ExpenseCategorySlot),
            _ when code.StartsWith(IncomeCategorySlot, StringComparison.Ordinal) => Category(code, data, IncomeCategorySlot),
            _ when code.StartsWith(RecentItemSlot, StringComparison.Ordinal) => Recent(code, data),
            _ when code.StartsWith(RecentCategorySlot, StringComparison.Ordinal) => Category(code, data, RecentCategorySlot),
            _ => throw new InvalidOperationException($"Workspace action '{code}' is not recognized")
        };
    }

    private static TelegramButton Account(string code, WorkspaceData data, string prefix)
    {
        OptionData item = WorkspaceBody.Option(data.Choices.Accounts, code, prefix);
        string text = string.IsNullOrWhiteSpace(item.Note) ? item.Name : $"{item.Name} · {item.Note}";
        return new TelegramButton(text, code);
    }

    private TelegramButton Category(string code, WorkspaceData data, string prefix)
    {
        OptionData item = WorkspaceBody.Option(data.Choices.Categories, code, prefix);
        return new TelegramButton(html.Category(item.Name, item.Note), code);
    }

    private TelegramButton Recent(string code, WorkspaceData data)
    {
        RecentItemData item = WorkspaceBody.Recent(data.Recent.Items, code, RecentItemSlot);
        string text = $"{item.Slot}. {WorkspaceHtml.Flow(item.Kind)} {html.Category(item.Category.Name, item.Category.Note)} · {html.Label(item.Amount, item.Currency)}";
        return new TelegramButton(text, code);
    }
}
