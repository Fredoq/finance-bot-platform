using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal static class WorkspaceScreen
{
    private const string ExpenseAccountSlot = "transaction.expense.account.";
    private const string ExpenseCategorySlot = "transaction.expense.category.";
    private const string IncomeAccountSlot = "transaction.income.account.";
    private const string IncomeCategorySlot = "transaction.income.category.";
    private const string RecentItemSlot = "transaction.recent.item.";
    private const string RecentCategorySlot = "transaction.recent.category.";
    private const string Primary = "primary";
    private const string Success = "success";
    private const string Danger = "danger";
    private static readonly Dictionary<string, string> icon = new(StringComparer.Ordinal)
    {
        ["food"] = "🍽",
        ["transport"] = "🚌",
        ["home"] = "🏠",
        ["health"] = "❤️",
        ["shopping"] = "🛍",
        ["fun"] = "🎉",
        ["bills"] = "🧾",
        ["travel"] = "✈",
        ["salary"] = "💼",
        ["bonus"] = "🏅",
        ["gift"] = "🎁",
        ["cashback"] = "💳",
        ["sale"] = "🏷",
        ["interest"] = "📈",
        ["refund"] = "↩",
        ["other"] = "➕"
    };
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static readonly NumberFormatInfo money = Note();
    public static TelegramText Message(long chatId, WorkspaceViewRequestedCommand command, ITelegramKeys keys)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(keys);
        WorkspaceData data = Data(command.Frame.State, command.Frame.StateData);
        return new TelegramText(chatId, Text(command.Frame.State, command.Freshness.IsNewUser, data), Keys(command.Frame.Actions, data), keys);
    }
    private static string Text(string state, bool fresh, WorkspaceData data) => state switch
    {
        "account.name" => Name(data),
        "account.currency" => Currency(data),
        "account.balance" => Balance(data),
        "account.confirm" => Confirm(data),
        "transaction.expense.account" => ExpenseAccount(data),
        "transaction.expense.amount" => ExpenseAmount(data),
        "transaction.expense.category" => ExpenseCategory(data),
        "transaction.expense.confirm" => ExpenseConfirm(data),
        "transaction.income.account" => IncomeAccount(data),
        "transaction.income.amount" => IncomeAmount(data),
        "transaction.income.category" => IncomeCategory(data),
        "transaction.income.confirm" => IncomeConfirm(data),
        "transaction.recent.list" => RecentList(data),
        "transaction.recent.detail" => RecentDetail(data),
        "transaction.recent.delete.confirm" => RecentDelete(data),
        "transaction.recent.category" => RecentCategory(data),
        "transaction.recent.recategorize.confirm" => RecentRecategorize(data),
        _ => Home(fresh, data)
    };
    private static string Home(bool fresh, WorkspaceData data)
    {
        var text = new StringBuilder();
        if (data.Accounts.Count == 0)
        {
            text.AppendLine("<b>Finance workspace</b>");
            if (!string.IsNullOrWhiteSpace(data.Status.Notice))
            {
                text.AppendLine(Escape(data.Status.Notice));
            }
            text.Append(fresh ? "Add your first account to start tracking your balance" : "Add an account to start tracking your balance");
            return text.ToString().TrimEnd();
        }
        text.AppendLine("<b>Your accounts</b>");
        foreach (AccountData item in data.Accounts)
        {
            text.AppendLine($"- <b>{Escape(item.Name)}</b>: {Amount(item.Amount, item.Currency)}");
        }
        if (!string.IsNullOrWhiteSpace(data.Status.Notice))
        {
            text.AppendLine(Escape(data.Status.Notice));
        }
        text.Append("Choose the next action");
        return text.ToString().TrimEnd();
    }
    private static string Name(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.Append("Send the account name");
        return text.ToString().TrimEnd();
    }
    private static string Currency(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.AppendLine($"Name: <b>{Escape(data.Financial.Name)}</b>");
        text.Append(data.Custom ? "Send a 3 letter currency code" : "Choose the account currency");
        return text.ToString().TrimEnd();
    }
    private static string Balance(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.AppendLine($"Name: <b>{Escape(data.Financial.Name)}</b>");
        text.AppendLine($"Currency: {Code(data.Financial.Currency)}");
        text.Append("Send the current balance");
        return text.ToString().TrimEnd();
    }
    private static string Confirm(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>Confirm account</b>");
        text.AppendLine($"Name: <b>{Escape(data.Financial.Name)}</b>");
        text.AppendLine($"Currency: {Code(data.Financial.Currency)}");
        text.Append($"Balance: <b>{Amount(data.Financial.Amount, data.Financial.Currency)}</b>");
        return text.ToString().TrimEnd();
    }
    private static string ExpenseAccount(WorkspaceData data) => Transaction("New expense", data, data.Expense, static (text, _) => text.Append("Choose the account"));
    private static string ExpenseAmount(WorkspaceData data) => Transaction("New expense", data, data.Expense, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Currency: {Code(item.Account.Note)}");
        text.Append("Send the amount");
    });
    private static string ExpenseCategory(WorkspaceData data) => Transaction("New expense", data, data.Expense, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Amount: <b>{Amount(item.Amount, item.Account.Note)}</b>");
        text.Append("Choose the category or send a new name");
    });
    private static string ExpenseConfirm(WorkspaceData data) => Transaction("Confirm expense", data, data.Expense, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{Escape(Category(item.Category.Name, item.Category.Note))}</b>");
        text.Append($"Amount: <b>{Amount(item.Amount, item.Account.Note)}</b>");
    });
    private static string IncomeAccount(WorkspaceData data) => Transaction("New income", data, data.Income, static (text, _) => text.Append("Choose the account"));
    private static string IncomeAmount(WorkspaceData data) => Transaction("New income", data, data.Income, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Currency: {Code(item.Account.Note)}");
        text.Append("Send the amount");
    });
    private static string IncomeCategory(WorkspaceData data) => Transaction("New income", data, data.Income, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Amount: <b>{Amount(item.Amount, item.Account.Note)}</b>");
        text.Append("Choose the category or send a new name");
    });
    private static string IncomeConfirm(WorkspaceData data) => Transaction("Confirm income", data, data.Income, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{Escape(Category(item.Category.Name, item.Category.Note))}</b>");
        text.Append($"Amount: <b>{Amount(item.Amount, item.Account.Note)}</b>");
    });
    private static IReadOnlyList<TelegramRow> Keys(IReadOnlyList<string> actions, WorkspaceData data)
    {
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(data);
        TelegramButton[] item = [.. actions.Select(code => Button(code, data))];
        return [.. item.Chunk(2).Select(row => new TelegramRow([.. row]))];
    }
    private static TelegramButton Button(string code, WorkspaceData data)
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
            "account.cancel" => new TelegramButton("✖ Cancel", code, Danger),
            "transaction.expense.add" => new TelegramButton("➖ Add expense", code, Primary),
            "transaction.income.add" => new TelegramButton("➕ Add income", code, Primary),
            "transaction.expense.create" => new TelegramButton("✅ Save expense", code, Success),
            "transaction.expense.cancel" => new TelegramButton("✖ Cancel", code, Danger),
            "transaction.income.create" => new TelegramButton("✅ Save income", code, Success),
            "transaction.income.cancel" => new TelegramButton("✖ Cancel", code, Danger),
            "transaction.recent.show" => new TelegramButton("🧾 Recent transactions", code, Primary),
            "transaction.recent.page.prev" => new TelegramButton("◀ Previous", code),
            "transaction.recent.page.next" => new TelegramButton("Next ▶", code),
            "transaction.recent.back" => new TelegramButton("↩ Back", code),
            "transaction.recent.delete" => new TelegramButton("🗑 Delete", code, Danger),
            "transaction.recent.delete.apply" => new TelegramButton("✅ Delete transaction", code, Danger),
            "transaction.recent.recategorize" => new TelegramButton("✏ Change category", code, Primary),
            "transaction.recent.recategorize.apply" => new TelegramButton("✅ Save category", code, Success),
            _ when code.StartsWith(ExpenseAccountSlot, StringComparison.Ordinal) => AccountButton(code, data, ExpenseAccountSlot),
            _ when code.StartsWith(IncomeAccountSlot, StringComparison.Ordinal) => AccountButton(code, data, IncomeAccountSlot),
            _ when code.StartsWith(ExpenseCategorySlot, StringComparison.Ordinal) => CategoryButton(code, data, ExpenseCategorySlot),
            _ when code.StartsWith(IncomeCategorySlot, StringComparison.Ordinal) => CategoryButton(code, data, IncomeCategorySlot),
            _ when code.StartsWith(RecentItemSlot, StringComparison.Ordinal) => RecentButton(code, data),
            _ when code.StartsWith(RecentCategorySlot, StringComparison.Ordinal) => CategoryButton(code, data, RecentCategorySlot),
            _ => new TelegramButton(code, code)
        };
    }
    private static TelegramButton AccountButton(string code, WorkspaceData data, string prefix)
    {
        OptionData item = Option(data.Choices.Accounts, code, prefix);
        string text = string.IsNullOrWhiteSpace(item.Note) ? item.Name : $"{item.Name} · {item.Note}";
        return new TelegramButton(text, code);
    }
    private static TelegramButton CategoryButton(string code, WorkspaceData data, string prefix)
    {
        OptionData item = Option(data.Choices.Categories, code, prefix);
        return new TelegramButton(Category(item.Name, item.Note), code);
    }
    private static TelegramButton RecentButton(string code, WorkspaceData data)
    {
        RecentItemData item = Recent(data.Recent.Items, code, RecentItemSlot);
        string text = $"{item.Slot}. {Flow(item.Kind)} {Category(item.Category.Name, item.Category.Note)} · {Label(item.Amount, item.Currency)}";
        return new TelegramButton(text, code);
    }
    private static string Transaction(string title, WorkspaceData data, TransactionData item, Action<StringBuilder, TransactionData> write)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine($"<b>{title}</b>");
        write(text, item);
        return text.ToString().TrimEnd();
    }
    private static string RecentList(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>Recent transactions</b>");
        if (data.Recent.Items.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(data.Status.Notice))
            {
                text.AppendLine(Escape(data.Status.Notice));
            }
            text.Append("No transactions yet");
            return text.ToString().TrimEnd();
        }
        foreach (RecentItemData item in data.Recent.Items)
        {
            text.AppendLine($"{item.Slot}. {Escape(Category(item.Category.Name, item.Category.Note))} · {Amount(item.Amount, item.Currency)}");
            text.AppendLine($"   {Escape(item.Account.Name)} · {Escape(When(item.OccurredUtc))}");
        }
        if (!string.IsNullOrWhiteSpace(data.Status.Notice))
        {
            text.AppendLine(Escape(data.Status.Notice));
        }
        text.Append($"Page {data.Recent.Page + 1}");
        return text.ToString().TrimEnd();
    }
    private static string RecentDetail(WorkspaceData data) => RecentText("Transaction", data, static (text, item) =>
    {
        text.AppendLine($"Kind: <b>{Escape(Title(item.Kind))}</b>");
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{Escape(Category(item.Category.Name, item.Category.Note))}</b>");
        text.AppendLine($"Amount: <b>{Amount(item.Amount, item.Currency)}</b>");
        text.Append($"Recorded: <code>{Escape(When(item.OccurredUtc))}</code>");
    });
    private static string RecentDelete(WorkspaceData data) => RecentText("Delete transaction", data, static (text, item) =>
    {
        text.AppendLine($"Category: <b>{Escape(Category(item.Category.Name, item.Category.Note))}</b>");
        text.AppendLine($"Amount: <b>{Amount(item.Amount, item.Currency)}</b>");
        text.Append("Delete this transaction");
    });
    private static string RecentCategory(WorkspaceData data) => RecentText("Change category", data, static (text, item) =>
    {
        text.AppendLine($"Current: <b>{Escape(Category(item.Category.Name, item.Category.Note))}</b>");
        text.AppendLine($"Amount: <b>{Amount(item.Amount, item.Currency)}</b>");
        text.Append("Choose the category or send a new name");
    });
    private static string RecentRecategorize(WorkspaceData data) => RecentText("Confirm category", data, static (text, item) =>
    {
        text.AppendLine($"Account: <b>{Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{Escape(Category(item.Category.Name, item.Category.Note))}</b>");
        text.Append($"Amount: <b>{Amount(item.Amount, item.Currency)}</b>");
    });
    private static string RecentText(string title, WorkspaceData data, Action<StringBuilder, RecentItemData> write)
    {
        RecentItemData item = Selected(data);
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine($"<b>{title}</b>");
        write(text, item);
        return text.ToString().TrimEnd();
    }
    private static WorkspaceData Data(string state, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Workspace screen '{state}' requires StateData");
        }
        WorkspaceData? item;
        try
        {
            item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException($"Workspace screen '{state}' has invalid StateData", error);
        }
        if (item is null)
        {
            throw new InvalidOperationException($"Workspace screen '{state}' has invalid StateData");
        }
        return state switch
        {
            "home" => item,
            "account.name" => item,
            "account.currency" => CurrencyData(item),
            "account.balance" => BalanceData(item),
            "account.confirm" => ConfirmData(item),
            "transaction.expense.account" => ExpenseAccountData(item),
            "transaction.expense.amount" => ExpenseAmountData(item),
            "transaction.expense.category" => ExpenseCategoryData(item),
            "transaction.expense.confirm" => ExpenseConfirmData(item),
            "transaction.income.account" => IncomeAccountData(item),
            "transaction.income.amount" => IncomeAmountData(item),
            "transaction.income.category" => IncomeCategoryData(item),
            "transaction.income.confirm" => IncomeConfirmData(item),
            "transaction.recent.list" => RecentListData(item),
            "transaction.recent.detail" => RecentSelectedData(item, "transaction.recent.detail"),
            "transaction.recent.delete.confirm" => RecentSelectedData(item, "transaction.recent.delete.confirm"),
            "transaction.recent.category" => RecentCategoryData(item),
            "transaction.recent.recategorize.confirm" => RecentSelectedData(item, "transaction.recent.recategorize.confirm"),
            _ => item
        };
    }
    private static WorkspaceData CurrencyData(WorkspaceData item) => !string.IsNullOrWhiteSpace(item.Financial.Name) ? item : throw new InvalidOperationException("Workspace screen 'account.currency' requires account name");
    private static WorkspaceData BalanceData(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Financial.Name))
        {
            throw new InvalidOperationException("Workspace screen 'account.balance' requires account name");
        }
        return !string.IsNullOrWhiteSpace(item.Financial.Currency) ? item : throw new InvalidOperationException("Workspace screen 'account.balance' requires currency");
    }
    private static WorkspaceData ConfirmData(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Financial.Name))
        {
            throw new InvalidOperationException("Workspace screen 'account.confirm' requires account name");
        }
        if (string.IsNullOrWhiteSpace(item.Financial.Currency))
        {
            throw new InvalidOperationException("Workspace screen 'account.confirm' requires currency");
        }
        return item.Financial.Amount.HasValue ? item : throw new InvalidOperationException("Workspace screen 'account.confirm' requires amount");
    }
    private static WorkspaceData ExpenseAccountData(WorkspaceData item) => item.Choices.Accounts.Count > 0 ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.account' requires account choices");
    private static WorkspaceData ExpenseAmountData(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Expense.Account.Name))
        {
            throw new InvalidOperationException("Workspace screen 'transaction.expense.amount' requires account");
        }
        return !string.IsNullOrWhiteSpace(item.Expense.Account.Note) ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.amount' requires currency");
    }
    private static WorkspaceData ExpenseCategoryData(WorkspaceData item)
    {
        ExpenseAmountData(item);
        return item.Expense.Amount.HasValue ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.category' requires amount");
    }
    private static WorkspaceData ExpenseConfirmData(WorkspaceData item)
    {
        ExpenseCategoryData(item);
        return !string.IsNullOrWhiteSpace(item.Expense.Category.Name) ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.confirm' requires category");
    }
    private static WorkspaceData IncomeAccountData(WorkspaceData item) => item.Choices.Accounts.Count > 0 ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.account' requires account choices");
    private static WorkspaceData IncomeAmountData(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Income.Account.Name))
        {
            throw new InvalidOperationException("Workspace screen 'transaction.income.amount' requires account");
        }
        return !string.IsNullOrWhiteSpace(item.Income.Account.Note) ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.amount' requires currency");
    }
    private static WorkspaceData IncomeCategoryData(WorkspaceData item)
    {
        IncomeAmountData(item);
        return item.Income.Amount.HasValue ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.category' requires amount");
    }
    private static WorkspaceData IncomeConfirmData(WorkspaceData item)
    {
        IncomeCategoryData(item);
        return !string.IsNullOrWhiteSpace(item.Income.Category.Name) ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.confirm' requires category");
    }
    private static WorkspaceData RecentListData(WorkspaceData item) => item;
    private static WorkspaceData RecentCategoryData(WorkspaceData item)
    {
        RecentSelectedData(item, "transaction.recent.category");
        return item.Choices.Categories.Count > 0 ? item : throw new InvalidOperationException("Workspace screen 'transaction.recent.category' requires category choices");
    }
    private static WorkspaceData RecentSelectedData(WorkspaceData item, string state) => !string.IsNullOrWhiteSpace(item.Recent.Selected.Id) ? item : throw new InvalidOperationException($"Workspace screen '{state}' requires selected transaction");
    private static OptionData Option(IReadOnlyList<OptionData> list, string code, string prefix)
    {
        int slot = Slot(code, prefix);
        OptionData? item = list.SingleOrDefault(candidate => candidate.Slot == slot);
        return item ?? throw new InvalidOperationException($"Workspace button '{code}' is missing from StateData");
    }
    private static RecentItemData Recent(IReadOnlyList<RecentItemData> list, string code, string prefix)
    {
        int slot = Slot(code, prefix);
        RecentItemData? item = list.SingleOrDefault(candidate => candidate.Slot == slot);
        return item ?? throw new InvalidOperationException($"Workspace button '{code}' is missing from StateData");
    }
    private static RecentItemData Selected(WorkspaceData data) => !string.IsNullOrWhiteSpace(data.Recent.Selected.Id) ? data.Recent.Selected : throw new InvalidOperationException("Workspace screen requires selected transaction");
    private static int Slot(string value, string prefix)
    {
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return 0;
        }
        return int.TryParse(value[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && slot > 0 ? slot : 0;
    }
    private static string Amount(decimal? value, string code)
    {
        if (!value.HasValue)
        {
            throw new InvalidOperationException("Workspace amount is required");
        }
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"{Money(value.Value)} <code>{Escape(code)}</code>" : $"{Money(value.Value)} {sign} (<code>{Escape(code)}</code>)";
    }
    private static string Code(string code)
    {
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"<code>{Escape(code)}</code>" : $"{sign} <code>{Escape(code)}</code>";
    }
    private static string Label(decimal value, string code)
    {
        string sign = Sign(code);
        return string.IsNullOrWhiteSpace(sign) ? $"{Money(value)} {code}" : $"{Money(value)} {sign}";
    }
    private static string Sign(string code) => code.ToUpperInvariant() switch
    {
        "RUB" => "₽",
        "USD" => "$",
        "EUR" => "€",
        _ => string.Empty
    };
    private static string Flow(string kind) => string.Equals(kind, "income", StringComparison.Ordinal) ? "+" : "-";
    private static string Title(string kind) => string.Equals(kind, "income", StringComparison.Ordinal) ? "Income" : "Expense";
    private static string When(DateTimeOffset value) => value == default ? "unknown" : $"{value:yyyy-MM-dd HH:mm} UTC";
    private static string Category(string name, string code) => icon.TryGetValue(code, out string? value) ? $"{value} {name}" : name;
    private static string Escape(string value) => WebUtility.HtmlEncode(value);
    private static string Money(decimal value) => value.ToString("#,0.##", money);
    private static NumberFormatInfo Note()
    {
        var item = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        item.NumberGroupSeparator = " ";
        item.NumberDecimalSeparator = ".";
        return item;
    }
}
