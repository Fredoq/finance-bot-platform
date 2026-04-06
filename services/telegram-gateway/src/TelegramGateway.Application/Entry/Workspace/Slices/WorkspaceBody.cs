using System.Globalization;
using System.Text.Json;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class WorkspaceBody
{
    private readonly JsonSerializerOptions json;

    public WorkspaceBody() => json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    internal WorkspaceData Data(string state, string value)
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
            "account.currency" => Currency(item),
            "account.balance" => Balance(item),
            "account.confirm" => Confirm(item),
            "transaction.expense.account" => ExpenseAccount(item),
            "transaction.expense.amount" => ExpenseAmount(item),
            "transaction.expense.source" => ExpenseSource(item),
            "transaction.expense.category" => ExpenseCategory(item),
            "transaction.expense.confirm" => ExpenseConfirm(item),
            "transaction.income.account" => IncomeAccount(item),
            "transaction.income.amount" => IncomeAmount(item),
            "transaction.income.source" => IncomeSource(item),
            "transaction.income.category" => IncomeCategory(item),
            "transaction.income.confirm" => IncomeConfirm(item),
            "transaction.recent.list" => item,
            "transaction.recent.detail" => Selected(item, "transaction.recent.detail"),
            "transaction.recent.delete.confirm" => Selected(item, "transaction.recent.delete.confirm"),
            "transaction.recent.category" => RecentCategory(item),
            "transaction.recent.recategorize.confirm" => Selected(item, "transaction.recent.recategorize.confirm"),
            "summary.month" => Summary(item),
            "category.month" => Breakdown(item),
            _ => throw new InvalidOperationException($"Workspace screen '{state}' is not recognized")
        };
    }

    private static WorkspaceData Currency(WorkspaceData item) => !string.IsNullOrWhiteSpace(item.Financial.Name) ? item : throw new InvalidOperationException("Workspace screen 'account.currency' requires account name");

    private static WorkspaceData Balance(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Financial.Name))
        {
            throw new InvalidOperationException("Workspace screen 'account.balance' requires account name");
        }
        return !string.IsNullOrWhiteSpace(item.Financial.Currency) ? item : throw new InvalidOperationException("Workspace screen 'account.balance' requires currency");
    }

    private static WorkspaceData Confirm(WorkspaceData item)
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

    private static WorkspaceData ExpenseAccount(WorkspaceData item) => item.Choices.Accounts.Count > 0 ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.account' requires account choices");

    private static WorkspaceData ExpenseAmount(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Expense.Account.Name))
        {
            throw new InvalidOperationException("Workspace screen 'transaction.expense.amount' requires account");
        }
        return !string.IsNullOrWhiteSpace(item.Expense.Account.Note) ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.amount' requires currency");
    }

    private static WorkspaceData ExpenseCategory(WorkspaceData item)
    {
        ExpenseSource(item);
        if (!item.Expense.Amount.HasValue)
        {
            throw new InvalidOperationException("Workspace screen 'transaction.expense.category' requires amount");
        }
        return !string.IsNullOrWhiteSpace(item.Expense.Source) ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.category' requires source");
    }

    private static WorkspaceData ExpenseSource(WorkspaceData item) => ExpenseAmount(item);

    private static WorkspaceData ExpenseConfirm(WorkspaceData item)
    {
        ExpenseCategory(item);
        if (string.IsNullOrWhiteSpace(item.Expense.Source))
        {
            throw new InvalidOperationException("Workspace screen 'transaction.expense.confirm' requires source");
        }
        return !string.IsNullOrWhiteSpace(item.Expense.Category.Name) ? item : throw new InvalidOperationException("Workspace screen 'transaction.expense.confirm' requires category");
    }

    private static WorkspaceData IncomeAccount(WorkspaceData item) => item.Choices.Accounts.Count > 0 ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.account' requires account choices");

    private static WorkspaceData IncomeAmount(WorkspaceData item)
    {
        if (string.IsNullOrWhiteSpace(item.Income.Account.Name))
        {
            throw new InvalidOperationException("Workspace screen 'transaction.income.amount' requires account");
        }
        return !string.IsNullOrWhiteSpace(item.Income.Account.Note) ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.amount' requires currency");
    }

    private static WorkspaceData IncomeCategory(WorkspaceData item)
    {
        IncomeSource(item);
        if (!item.Income.Amount.HasValue)
        {
            throw new InvalidOperationException("Workspace screen 'transaction.income.category' requires amount");
        }
        return !string.IsNullOrWhiteSpace(item.Income.Source) ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.category' requires source");
    }

    private static WorkspaceData IncomeSource(WorkspaceData item) => IncomeAmount(item);

    private static WorkspaceData IncomeConfirm(WorkspaceData item)
    {
        IncomeCategory(item);
        if (string.IsNullOrWhiteSpace(item.Income.Source))
        {
            throw new InvalidOperationException("Workspace screen 'transaction.income.confirm' requires source");
        }
        return !string.IsNullOrWhiteSpace(item.Income.Category.Name) ? item : throw new InvalidOperationException("Workspace screen 'transaction.income.confirm' requires category");
    }

    private static WorkspaceData RecentCategory(WorkspaceData item)
    {
        Selected(item, "transaction.recent.category");
        return item.Choices.Categories.Count > 0 ? item : throw new InvalidOperationException("Workspace screen 'transaction.recent.category' requires category choices");
    }

    private static WorkspaceData Summary(WorkspaceData item)
    {
        if (item.Summary.Year <= 0)
        {
            throw new InvalidOperationException("Workspace screen 'summary.month' requires year");
        }
        if (item.Summary.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Workspace screen 'summary.month' requires month");
        }
        return item;
    }

    private static WorkspaceData Breakdown(WorkspaceData item)
    {
        if (item.Breakdown.Year <= 0)
        {
            throw new InvalidOperationException("Workspace screen 'category.month' requires year");
        }
        if (item.Breakdown.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Workspace screen 'category.month' requires month");
        }
        if (item.Breakdown.Currencies is null)
        {
            throw new InvalidOperationException("Workspace screen 'category.month' requires currencies");
        }
        foreach (BreakdownCurrencyData currency in item.Breakdown.Currencies)
        {
            if (currency is null)
            {
                throw new InvalidOperationException("Workspace screen 'category.month' has invalid currencies");
            }
            if (currency.Categories is null)
            {
                throw new InvalidOperationException("Workspace screen 'category.month' requires categories");
            }
            if (currency.Categories.Any(category => category is null))
            {
                throw new InvalidOperationException("Workspace screen 'category.month' has invalid categories");
            }
        }
        return item;
    }

    internal static OptionData Option(IReadOnlyList<OptionData> list, string code, string prefix)
    {
        int slot = Slot(code, prefix);
        OptionData? item = list.SingleOrDefault(candidate => candidate.Slot == slot);
        return item ?? throw new InvalidOperationException($"Workspace button '{code}' is missing from StateData");
    }

    internal static RecentItemData Recent(IReadOnlyList<RecentItemData> list, string code, string prefix)
    {
        int slot = Slot(code, prefix);
        RecentItemData? item = list.SingleOrDefault(candidate => candidate.Slot == slot);
        return item ?? throw new InvalidOperationException($"Workspace button '{code}' is missing from StateData");
    }

    internal static RecentItemData Selected(WorkspaceData data) => !string.IsNullOrWhiteSpace(data.Recent.Selected.Id) ? data.Recent.Selected : throw new InvalidOperationException("Workspace screen requires selected transaction");

    private static WorkspaceData Selected(WorkspaceData item, string state) => !string.IsNullOrWhiteSpace(item.Recent.Selected.Id) ? item : throw new InvalidOperationException($"Workspace screen '{state}' requires selected transaction");

    private static int Slot(string value, string prefix)
    {
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return 0;
        }
        return int.TryParse(value[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && slot > 0 ? slot : 0;
    }
}
