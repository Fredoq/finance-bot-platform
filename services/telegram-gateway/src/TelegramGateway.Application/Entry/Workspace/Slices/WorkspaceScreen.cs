using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using TelegramGateway.Application.Telegram.Delivery;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal static class WorkspaceScreen
{
    private const string AccountSlot = "transaction.expense.account.";
    private const string CategorySlot = "transaction.expense.category.";
    private static readonly Dictionary<string, string> icon = new(StringComparer.Ordinal)
    {
        ["food"] = "🍽",
        ["transport"] = "🚌",
        ["home"] = "🏠",
        ["health"] = "❤️",
        ["shopping"] = "🛍",
        ["fun"] = "🎉",
        ["bills"] = "🧾",
        ["travel"] = "✈"
    };
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static readonly NumberFormatInfo money = Note();
    public static TelegramText Message(long chatId, WorkspaceViewRequestedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        WorkspaceData data = Data(command.Frame.State, command.Frame.StateData);
        return new TelegramText(chatId, Text(command.Frame.State, command.Freshness.IsNewUser, data), Keys(command.Frame.Actions, data));
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
    private static string ExpenseAccount(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>New expense</b>");
        text.Append("Choose the account");
        return text.ToString().TrimEnd();
    }
    private static string ExpenseAmount(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>New expense</b>");
        text.AppendLine($"Account: <b>{Escape(data.Expense.Account.Name)}</b>");
        text.AppendLine($"Currency: {Code(data.Expense.Account.Note)}");
        text.Append("Send the amount");
        return text.ToString().TrimEnd();
    }
    private static string ExpenseCategory(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>New expense</b>");
        text.AppendLine($"Account: <b>{Escape(data.Expense.Account.Name)}</b>");
        text.AppendLine($"Amount: <b>{Amount(data.Expense.Amount, data.Expense.Account.Note)}</b>");
        text.Append("Choose the category or send a new name");
        return text.ToString().TrimEnd();
    }
    private static string ExpenseConfirm(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(Escape(data.Status.Error));
        }
        text.AppendLine("<b>Confirm expense</b>");
        text.AppendLine($"Account: <b>{Escape(data.Expense.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{Escape(Category(data.Expense.Category.Name, data.Expense.Category.Note))}</b>");
        text.Append($"Amount: <b>{Amount(data.Expense.Amount, data.Expense.Account.Note)}</b>");
        return text.ToString().TrimEnd();
    }
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
            "account.add" => new TelegramButton("➕ Add account", code, "primary"),
            "account.currency.rub" => new TelegramButton("RUB ₽", code),
            "account.currency.usd" => new TelegramButton("USD $", code),
            "account.currency.eur" => new TelegramButton("EUR €", code),
            "account.currency.other" => new TelegramButton("Other", code),
            "account.create" => new TelegramButton("✅ Create account", code, "success"),
            "account.cancel" => new TelegramButton("✖ Cancel", code, "danger"),
            "transaction.expense.add" => new TelegramButton("➖ Add expense", code, "primary"),
            "transaction.expense.create" => new TelegramButton("✅ Save expense", code, "success"),
            "transaction.expense.cancel" => new TelegramButton("✖ Cancel", code, "danger"),
            _ when code.StartsWith(AccountSlot, StringComparison.Ordinal) => AccountButton(code, data),
            _ when code.StartsWith(CategorySlot, StringComparison.Ordinal) => CategoryButton(code, data),
            _ => new TelegramButton(code, code)
        };
    }
    private static TelegramButton AccountButton(string code, WorkspaceData data)
    {
        OptionData item = Option(data.Choices.Accounts, code, AccountSlot);
        string text = string.IsNullOrWhiteSpace(item.Note) ? item.Name : $"{item.Name} · {item.Note}";
        return new TelegramButton(text, code);
    }
    private static TelegramButton CategoryButton(string code, WorkspaceData data)
    {
        OptionData item = Option(data.Choices.Categories, code, CategorySlot);
        return new TelegramButton(Category(item.Name, item.Note), code);
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
    private static OptionData Option(IReadOnlyList<OptionData> list, string code, string prefix)
    {
        int slot = Slot(code, prefix);
        OptionData? item = list.SingleOrDefault(candidate => candidate.Slot == slot);
        return item ?? throw new InvalidOperationException($"Workspace button '{code}' is missing from StateData");
    }
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
    private static string Sign(string code) => code.ToUpperInvariant() switch
    {
        "RUB" => "₽",
        "USD" => "$",
        "EUR" => "€",
        _ => string.Empty
    };
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
