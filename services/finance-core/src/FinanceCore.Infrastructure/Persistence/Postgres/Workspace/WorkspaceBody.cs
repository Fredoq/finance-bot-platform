#pragma warning disable S2325
using System.Globalization;
using System.Text.Json;
using FinanceCore.Domain.Workspace.Models;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceBody
{
    internal const string HomeState = "home";
    internal const string NameState = "account.name";
    internal const string CurrencyState = "account.currency";
    internal const string BalanceState = "account.balance";
    internal const string ConfirmState = "account.confirm";
    internal const string ExpenseAccountState = "transaction.expense.account";
    internal const string ExpenseAmountState = "transaction.expense.amount";
    internal const string ExpenseCategoryState = "transaction.expense.category";
    internal const string ExpenseConfirmState = "transaction.expense.confirm";
    internal const string IncomeAccountState = "transaction.income.account";
    internal const string IncomeAmountState = "transaction.income.amount";
    internal const string IncomeCategoryState = "transaction.income.category";
    internal const string IncomeConfirmState = "transaction.income.confirm";
    internal const string RecentListState = "transaction.recent.list";
    internal const string RecentDetailState = "transaction.recent.detail";
    internal const string RecentDeleteState = "transaction.recent.delete.confirm";
    internal const string RecentCategoryState = "transaction.recent.category";
    internal const string RecentRecategorizeState = "transaction.recent.recategorize.confirm";
    internal const string AddAccount = "account.add";
    internal const string AddExpense = "transaction.expense.add";
    internal const string AddIncome = "transaction.income.add";
    internal const string ShowRecent = "transaction.recent.show";
    internal const string AccountCancel = "account.cancel";
    internal const string ExpenseCancel = "transaction.expense.cancel";
    internal const string IncomeCancel = "transaction.income.cancel";
    internal const string CreateAccountCode = "account.create";
    internal const string CreateExpenseCode = "transaction.expense.create";
    internal const string CreateIncomeCode = "transaction.income.create";
    internal const string RecentBack = "transaction.recent.back";
    internal const string RecentDelete = "transaction.recent.delete";
    internal const string RecentDeleteApply = "transaction.recent.delete.apply";
    internal const string RecentRecategorize = "transaction.recent.recategorize";
    internal const string RecentRecategorizeApply = "transaction.recent.recategorize.apply";
    internal const string RecentPrevious = "transaction.recent.page.prev";
    internal const string RecentNext = "transaction.recent.page.next";
    internal const string Rub = "account.currency.rub";
    internal const string Usd = "account.currency.usd";
    internal const string Eur = "account.currency.eur";
    internal const string Other = "account.currency.other";
    internal const string ExpenseAccountSlot = "transaction.expense.account.";
    internal const string ExpenseCategorySlot = "transaction.expense.category.";
    internal const string IncomeAccountSlot = "transaction.income.account.";
    internal const string IncomeCategorySlot = "transaction.income.category.";
    internal const string RecentItemSlot = "transaction.recent.item.";
    internal const string RecentCategorySlot = "transaction.recent.category.";
    internal const int RecentPageSize = 5;
    internal const string ExpenseKind = "expense";
    internal const string IncomeKind = "income";
    internal const string DeleteMode = "delete";
    internal const string RecategorizeMode = "recategorize";
    internal const string AddAccountPrompt = "Tap Add account to start";
    internal const string ChooseActionPrompt = "Choose one action";
    internal const string ConfirmGoBackPrompt = "Use the buttons to confirm or go back";
    internal const string TransactionMissingNotice = "Transaction was not found";
    private readonly JsonSerializerOptions json;

    internal WorkspaceBody() => json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    internal WorkspaceData Reset(WorkspaceData body, string notice) => new(body.Accounts, new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), new StatusData(string.Empty, notice), false));

    internal WorkspaceData Home(IReadOnlyList<AccountData> list, string notice, string error = "") => new(list, new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), new StatusData(error, notice), false));

    internal WorkspaceData Sync(WorkspaceData body, IReadOnlyList<AccountData> list) => new(list, new WorkspaceStateData(body.Financial, body.Expense, body.Income, body.Recent, body.Choices, body.Status, body.Custom));

    internal WorkspaceData Data(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkspaceData();
        }
        WorkspaceData? item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        return new WorkspaceData(item?.Accounts ?? [], new WorkspaceStateData(item?.Financial ?? new FinancialData(), item?.Expense ?? new ExpenseData(), item?.Income ?? new IncomeData(), item?.Recent ?? new RecentData(), item?.Choices ?? new ChoicesData(), item?.Status ?? new StatusData(), item?.Custom ?? false));
    }

    internal string Json(WorkspaceData item) => JsonSerializer.Serialize(item, json);

    internal WorkspaceActionContext Context(WorkspaceData body) => new(body.Accounts.Count, body.Choices.Accounts.Count, body.Choices.Categories.Count, body.Recent.Items.Count, new RecentPaging(body.Recent.HasPrevious, body.Recent.HasNext), body.Custom);

    internal DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        return value.Offset == TimeSpan.Zero ? value : throw new ArgumentException("Workspace occurrence time must be UTC", name);
    }

    internal WorkspaceData Model(WorkspaceData body, FinancialData? financial = null, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData(financial ?? body.Financial, body.Expense, body.Income, body.Recent, choices ?? body.Choices, status ?? body.Status, body.Custom));

    internal WorkspaceData Account(WorkspaceData body, FinancialData financial, StatusData? status = null, bool custom = false) => new(body.Accounts, new WorkspaceStateData(financial, new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), status ?? new StatusData(), custom));

    internal WorkspaceData Transaction(WorkspaceData body, PickData account, PickData category, decimal? amount, bool income, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData(new FinancialData(), income ? new ExpenseData() : new ExpenseData(account, category, amount), income ? new IncomeData(account, category, amount) : new IncomeData(), new RecentData(), choices ?? new ChoicesData(), status ?? new StatusData(), false));

    internal WorkspaceData Recent(WorkspaceData body, RecentData recent, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), recent, choices ?? new ChoicesData(), status ?? new StatusData(), false));

    internal PickData Pick(WorkspaceData body, bool income) => income ? body.Income.Account : body.Expense.Account;

    internal PickData Category(WorkspaceData body, bool income) => income ? body.Income.Category : body.Expense.Category;

    internal decimal? Total(WorkspaceData body, bool income) => income ? body.Income.Amount : body.Expense.Amount;

    internal string Resolve(WorkspaceData body, bool income)
    {
        PickData account = Pick(body, income);
        if (!string.IsNullOrWhiteSpace(account.Id))
        {
            return account.Id;
        }
        AccountData? item = body.Accounts.FirstOrDefault(candidate => candidate.Name == account.Name && candidate.Currency == account.Note);
        return item?.Id ?? string.Empty;
    }

    internal int Slot(string value, string prefix)
    {
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return 0;
        }
        return int.TryParse(value[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && slot > 0 ? slot : 0;
    }

    internal bool AccountState(string state) => state is NameState or CurrencyState or BalanceState or ConfirmState;

    internal bool ExpenseState(string state) => state is ExpenseAccountState or ExpenseAmountState or ExpenseCategoryState or ExpenseConfirmState;

    internal bool IncomeState(string state) => state is IncomeAccountState or IncomeAmountState or IncomeCategoryState or IncomeConfirmState;

    internal bool RecentState(string state) => state is RecentListState or RecentDetailState or RecentDeleteState or RecentCategoryState or RecentRecategorizeState;

    internal bool TransactionCategoryState(string state) => state is ExpenseCategoryState or IncomeCategoryState or RecentCategoryState;

    internal OptionData? Option(IReadOnlyList<OptionData> list, int slot) => list.SingleOrDefault(item => item.Slot == slot);

    internal RecentItemData? Item(IReadOnlyList<RecentItemData> list, int slot) => list.SingleOrDefault(item => item.Slot == slot);

    internal IReadOnlyList<OptionData> Accounts(IReadOnlyList<AccountData> list) => [.. list.Select((item, index) => new OptionData(index + 1, item.Id, item.Name, item.Currency))];

    internal string Kind(string state) => state.StartsWith("transaction.income.", StringComparison.Ordinal) ? IncomeKind : ExpenseKind;

    internal string Kind(bool income) => income ? IncomeKind : ExpenseKind;

    internal string Supported(string kind) => kind switch
    {
        IncomeKind => IncomeKind,
        ExpenseKind => ExpenseKind,
        _ => throw new InvalidOperationException($"Transaction kind '{kind}' is not supported")
    };

    internal string Change(string kind) => Supported(kind) switch
    {
        IncomeKind => "+",
        ExpenseKind => "-",
        _ => throw new InvalidOperationException($"Transaction kind '{kind}' is not supported")
    };

    internal string Reverse(string kind) => Supported(kind) switch
    {
        IncomeKind => "-",
        ExpenseKind => "+",
        _ => throw new InvalidOperationException($"Transaction kind '{kind}' is not supported")
    };

    internal string AccountCode(bool income) => income ? IncomeAccountState : ExpenseAccountState;

    internal string AmountCode(bool income) => income ? IncomeAmountState : ExpenseAmountState;

    internal string CategoryCode(bool income) => income ? IncomeCategoryState : ExpenseCategoryState;

    internal string ConfirmCode(bool income) => income ? IncomeConfirmState : ExpenseConfirmState;

    internal string CreateCode(bool income) => income ? CreateIncomeCode : CreateExpenseCode;

    internal string AccountSlot(bool income) => income ? IncomeAccountSlot : ExpenseAccountSlot;

    internal string CategorySlot(bool income) => income ? IncomeCategorySlot : ExpenseCategorySlot;
}
#pragma warning restore S2325
