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
    internal const string ExpenseSourceState = "transaction.expense.source";
    internal const string ExpenseCategoryState = "transaction.expense.category";
    internal const string ExpenseConfirmState = "transaction.expense.confirm";
    internal const string IncomeAccountState = "transaction.income.account";
    internal const string IncomeAmountState = "transaction.income.amount";
    internal const string IncomeSourceState = "transaction.income.source";
    internal const string IncomeCategoryState = "transaction.income.category";
    internal const string IncomeConfirmState = "transaction.income.confirm";
    internal const string RecentListState = "transaction.recent.list";
    internal const string RecentDetailState = "transaction.recent.detail";
    internal const string RecentDeleteState = "transaction.recent.delete.confirm";
    internal const string RecentCategoryState = "transaction.recent.category";
    internal const string RecentRecategorizeState = "transaction.recent.recategorize.confirm";
    internal const string SummaryState = "summary.month";
    internal const string BreakdownState = "category.month";
    internal const string AddAccount = "account.add";
    internal const string AddExpense = "transaction.expense.add";
    internal const string AddIncome = "transaction.income.add";
    internal const string ShowRecent = "transaction.recent.show";
    internal const string ShowSummary = "summary.month.show";
    internal const string ShowBreakdown = "category.month.show";
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
    internal const string SummaryPrevious = "summary.month.prev";
    internal const string SummaryNext = "summary.month.next";
    internal const string SummaryBack = "summary.month.back";
    internal const string BreakdownPrevious = "category.month.prev";
    internal const string BreakdownNext = "category.month.next";
    internal const string BreakdownBack = "category.month.back";
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
    internal const string ChooseActionPrompt = "Choose the next action";
    internal const string ConfirmGoBackPrompt = "Use the buttons to confirm or go back";
    internal const string TransactionMissingNotice = "Transaction was not found";
    private readonly JsonSerializerOptions json;
    private readonly WorkspaceStateSet states;
    private readonly WorkspaceKindSet kinds;
    private readonly WorkspaceCodeSet codes;

    internal WorkspaceBody()
    {
        json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        states = new WorkspaceStateSet();
        kinds = new WorkspaceKindSet();
        codes = new WorkspaceCodeSet();
    }

    internal WorkspaceData Reset(WorkspaceData body, string notice) => new(body.Accounts, new WorkspaceStateData { Status = new StatusData(codes.Empty, notice), Custom = codes.Custom(false) });

    internal WorkspaceData Home(IReadOnlyList<AccountData> list, string notice, string error = "") => new(list, new WorkspaceStateData { Status = new StatusData(error, notice), Custom = codes.Custom(false) });

    internal WorkspaceData Sync(WorkspaceData body, IReadOnlyList<AccountData> list) => new(list, new WorkspaceStateData { Financial = body.Financial, Expense = body.Expense, Income = body.Income, Recent = body.Recent, Summary = body.Summary, Breakdown = body.Breakdown, Choices = body.Choices, Status = body.Status, Custom = codes.Custom(body.Custom) });

    internal WorkspaceData Data(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkspaceData();
        }
        WorkspaceData? item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        return new WorkspaceData(item?.Accounts ?? [], new WorkspaceStateData
        {
            Financial = item?.Financial ?? new FinancialData(),
            Expense = item?.Expense ?? new ExpenseData(),
            Income = item?.Income ?? new IncomeData(),
            Recent = item?.Recent ?? new RecentData(),
            Summary = item?.Summary ?? new SummaryData(),
            Breakdown = item?.Breakdown ?? new BreakdownData(),
            Choices = item?.Choices ?? new ChoicesData(),
            Status = item?.Status ?? new StatusData(),
            Custom = codes.Custom(item?.Custom ?? false)
        });
    }

    internal string Json(WorkspaceData item) => JsonSerializer.Serialize(item, json);

    internal WorkspaceActionContext Context(WorkspaceData body, DateTimeOffset when) => new(body.Accounts.Count, body.Choices.Accounts.Count, body.Choices.Categories.Count, body.Recent.Items.Count, new RecentPaging(body.Recent.HasPrevious, body.Recent.HasNext), new MonthPaging(SummaryHasNext(body.Summary, when), BreakdownHasNext(body.Breakdown, when)), codes.Custom(body.Custom));

    internal DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        return value.Offset == codes.Utc ? value : throw new ArgumentException("Workspace occurrence time must be UTC", name);
    }

    internal WorkspaceData Model(WorkspaceData body, FinancialData? financial = null, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData { Financial = financial ?? body.Financial, Expense = body.Expense, Income = body.Income, Recent = body.Recent, Summary = body.Summary, Breakdown = body.Breakdown, Choices = choices ?? body.Choices, Status = status ?? body.Status, Custom = codes.Custom(body.Custom) });

    internal WorkspaceData Account(WorkspaceData body, FinancialData financial, StatusData? status = null, bool custom = false) => new(body.Accounts, new WorkspaceStateData { Financial = financial, Status = status ?? new StatusData(), Custom = codes.Custom(custom) });

    internal WorkspaceData Transaction(WorkspaceData body, PickData account, PickData category, decimal? amount, bool income, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData
    {
        Expense = kinds.Income(income) ? new ExpenseData() : new ExpenseData(account, category, amount, string.Empty),
        Income = kinds.Income(income) ? new IncomeData(account, category, amount, string.Empty) : new IncomeData(),
        Summary = body.Summary,
        Breakdown = body.Breakdown,
        Choices = choices ?? new ChoicesData(),
        Status = status ?? new StatusData(),
        Custom = codes.Custom(false)
    });

    internal WorkspaceData Source(WorkspaceData body, string source, bool income) => new(body.Accounts, new WorkspaceStateData
    {
        Expense = kinds.Income(income) ? body.Expense : new ExpenseData(body.Expense.Account, body.Expense.Category, body.Expense.Amount, source),
        Income = kinds.Income(income) ? new IncomeData(body.Income.Account, body.Income.Category, body.Income.Amount, source) : body.Income,
        Summary = body.Summary,
        Breakdown = body.Breakdown,
        Choices = body.Choices,
        Status = body.Status,
        Custom = codes.Custom(false)
    });

    internal WorkspaceData Recent(WorkspaceData body, RecentData recent, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData { Recent = recent, Summary = body.Summary, Breakdown = body.Breakdown, Choices = choices ?? new ChoicesData(), Status = status ?? new StatusData(), Custom = codes.Custom(false) });

    internal WorkspaceData Summary(WorkspaceData body, SummaryData summary, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData { Summary = summary, Breakdown = body.Breakdown, Status = status ?? new StatusData(), Custom = codes.Custom(false) });

    internal WorkspaceData Breakdown(WorkspaceData body, BreakdownData breakdown, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData { Summary = body.Summary, Breakdown = breakdown, Status = status ?? new StatusData(), Custom = codes.Custom(false) });

    internal PickData Pick(WorkspaceData body, bool income) => kinds.Income(income) ? body.Income.Account : body.Expense.Account;

    internal PickData Category(WorkspaceData body, bool income) => kinds.Income(income) ? body.Income.Category : body.Expense.Category;

    internal string Value(WorkspaceData body, bool income) => kinds.Income(income) ? body.Income.Source : body.Expense.Source;

    internal decimal? Total(WorkspaceData body, bool income) => kinds.Income(income) ? body.Income.Amount : body.Expense.Amount;

    internal string Resolve(WorkspaceData body, bool income)
    {
        PickData account = Pick(body, income);
        if (!string.IsNullOrWhiteSpace(account.Id))
        {
            return account.Id;
        }
        AccountData? item = body.Accounts.FirstOrDefault(candidate => candidate.Name == account.Name && candidate.Currency == account.Note);
        return item?.Id ?? codes.Empty;
    }

    internal int Slot(string value, string prefix)
    {
        if (!value.StartsWith(prefix, codes.Comparison))
        {
            return codes.Zero;
        }
        return int.TryParse(value[prefix.Length..], codes.Styles, CultureInfo.InvariantCulture, out int slot) && slot > codes.Zero ? slot : codes.Zero;
    }

    internal bool AccountState(string state) => states.Account(state);

    internal bool ExpenseState(string state) => states.Expense(state);

    internal bool IncomeState(string state) => states.Income(state);

    internal bool RecentState(string state) => states.Recent(state);

    internal bool SummaryScreen(string state) => states.Summary(state);

    internal bool BreakdownScreen(string state) => states.Breakdown(state);

    internal bool TransactionCategoryState(string state) => states.Category(state);

    internal bool TransactionSourceState(string state) => states.Source(state);

    internal OptionData Option(IReadOnlyList<OptionData> list, int slot)
    {
        if (slot <= codes.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Option slot must be greater than zero");
        }
        return list.SingleOrDefault(item => item.Slot == slot) ?? throw new InvalidOperationException($"Option slot '{slot}' was not found");
    }

    internal RecentItemData Item(IReadOnlyList<RecentItemData> list, int slot)
    {
        if (slot <= codes.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Recent item slot must be greater than zero");
        }
        return list.SingleOrDefault(item => item.Slot == slot) ?? throw new InvalidOperationException($"Recent item slot '{slot}' was not found");
    }

    internal IReadOnlyList<OptionData> Accounts(IReadOnlyList<AccountData> list) => [.. list.Select((item, index) => new OptionData(index + 1 + codes.Zero, item.Id, item.Name, item.Currency))];

    internal string Kind(string state)
    {
        if (states.Income(state))
        {
            return kinds.Kind(true);
        }
        if (states.Expense(state))
        {
            return kinds.Kind(false);
        }
        throw new ArgumentException($"Workspace state '{state}' is not supported", nameof(state));
    }

    internal string Kind(bool income) => kinds.Kind(income);

    internal string Supported(string kind) => kinds.Supported(kind);

    internal string Change(string kind) => kinds.Change(kind);

    internal string Reverse(string kind) => kinds.Reverse(kind);

    internal string AccountCode(bool income) => codes.Account(income);

    internal string AmountCode(bool income) => codes.Amount(income);

    internal string CategoryCode(bool income) => codes.Category(income);

    internal string SourceCode(bool income) => codes.Source(income);

    internal string ConfirmCode(bool income) => codes.Confirm(income);

    internal string CreateCode(bool income) => codes.Create(income);

    internal string AccountSlot(bool income) => codes.Slot(income);

    internal string CategorySlot(bool income) => codes.CategorySlot(income);

    internal static SummaryData Month(DateTimeOffset when, string timeZone)
    {
        WorkspaceZone.MonthNote item = WorkspaceZone.Month(when, timeZone);
        return new SummaryData(item.Year, item.PeriodMonth, item.ZoneId, []);
    }

    internal static SummaryData Month(SummaryData data, int shift)
    {
        DateTimeOffset item = Start(data.Year, data.Month).AddMonths(shift);
        return new SummaryData(item.Year, item.Month, WorkspaceZone.Id(data.TimeZone), []);
    }

    internal static BreakdownData Month(BreakdownData data, int shift)
    {
        DateTimeOffset item = Start(data.Year, data.Month).AddMonths(shift);
        return new BreakdownData(item.Year, item.Month, WorkspaceZone.Id(data.TimeZone), []);
    }

    internal static bool SummaryHasNext(SummaryData data, DateTimeOffset when)
        => HasNext(data.Year, data.Month, when, data.TimeZone);

    internal static bool BreakdownHasNext(BreakdownData data, DateTimeOffset when)
        => HasNext(data.Year, data.Month, when, data.TimeZone);

    private static bool HasNext(int year, int month, DateTimeOffset when, string timeZone)
    {
        if (year <= 0 || month <= 0)
        {
            return false;
        }
        WorkspaceZone.MonthNote note = WorkspaceZone.Month(when, timeZone);
        DateTimeOffset current = Start(note.Year, note.PeriodMonth);
        DateTimeOffset selected = Start(year, month);
        return selected < current;
    }

    private static DateTimeOffset Start(int year, int month) => new(year, month, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class WorkspaceStateSet
    {
        private readonly HashSet<string> accounts;
        private readonly HashSet<string> expenses;
        private readonly HashSet<string> incomes;
        private readonly HashSet<string> recents;
        private readonly HashSet<string> summaries;
        private readonly HashSet<string> breakdowns;

        internal WorkspaceStateSet()
        {
            accounts = new HashSet<string>(StringComparer.Ordinal) { NameState, CurrencyState, BalanceState, ConfirmState };
            expenses = new HashSet<string>(StringComparer.Ordinal) { ExpenseAccountState, ExpenseAmountState, ExpenseSourceState, ExpenseCategoryState, ExpenseConfirmState };
            incomes = new HashSet<string>(StringComparer.Ordinal) { IncomeAccountState, IncomeAmountState, IncomeSourceState, IncomeCategoryState, IncomeConfirmState };
            recents = new HashSet<string>(StringComparer.Ordinal) { RecentListState, RecentDetailState, RecentDeleteState, RecentCategoryState, RecentRecategorizeState };
            summaries = new HashSet<string>(StringComparer.Ordinal) { SummaryState };
            breakdowns = new HashSet<string>(StringComparer.Ordinal) { BreakdownState };
        }

        internal bool Account(string state) => accounts.Contains(state);

        internal bool Category(string state) => expenses.Contains(state) && state == ExpenseCategoryState || incomes.Contains(state) && state == IncomeCategoryState || recents.Contains(state) && state == RecentCategoryState;

        internal bool Source(string state) => expenses.Contains(state) && state == ExpenseSourceState || incomes.Contains(state) && state == IncomeSourceState;

        internal bool Expense(string state) => expenses.Contains(state);

        internal bool Income(string state) => incomes.Contains(state);

        internal bool Recent(string state) => recents.Contains(state);

        internal bool Summary(string state) => summaries.Contains(state);

        internal bool Breakdown(string state) => breakdowns.Contains(state);
    }

    private sealed class WorkspaceKindSet
    {
        private readonly Dictionary<string, WorkspaceKindMark> marks;
        private readonly string income;
        private readonly string expense;
        private readonly bool flag;

        internal WorkspaceKindSet()
        {
            income = IncomeKind;
            expense = ExpenseKind;
            flag = true;
            marks = new Dictionary<string, WorkspaceKindMark>(StringComparer.Ordinal)
            {
                [income] = new WorkspaceKindMark("+", "-"),
                [expense] = new WorkspaceKindMark("-", "+")
            };
        }

        internal string Change(string kind) => marks[Supported(kind)].Change;

        internal bool Income(bool value) => value == flag;

        internal string Kind(bool value) => Income(value) ? income : expense;

        internal string Reverse(string kind) => marks[Supported(kind)].Reverse;

        internal string Supported(string kind) => marks.ContainsKey(kind) ? kind : throw new InvalidOperationException($"Transaction kind '{kind}' is not supported");
    }

    private sealed class WorkspaceKindMark
    {
        internal WorkspaceKindMark(string change, string reverse)
        {
            Change = change;
            Reverse = reverse;
        }

        internal string Change { get; }

        internal string Reverse { get; }
    }

    private sealed class WorkspaceCodeSet
    {
        private readonly WorkspacePath expense;
        private readonly WorkspacePath income;
        private readonly WorkspaceScalar scalar;

        internal WorkspaceCodeSet()
        {
            expense = new WorkspacePath([ExpenseAccountState, ExpenseAmountState, ExpenseSourceState, ExpenseCategoryState, ExpenseConfirmState, CreateExpenseCode], ExpenseAccountSlot, ExpenseCategorySlot);
            income = new WorkspacePath([IncomeAccountState, IncomeAmountState, IncomeSourceState, IncomeCategoryState, IncomeConfirmState, CreateIncomeCode], IncomeAccountSlot, IncomeCategorySlot);
            scalar = new WorkspaceScalar();
        }

        internal StringComparison Comparison => scalar.Comparison;

        internal string Empty => scalar.Empty;

        internal NumberStyles Styles => scalar.Styles;

        internal TimeSpan Utc => scalar.Utc;

        internal int Zero => scalar.Empty.Length;

        internal string Account(bool value) => Path(value).Account;

        internal string Amount(bool value) => Path(value).Amount;

        internal string Category(bool value) => Path(value).Category;

        internal string Source(bool value) => Path(value).Source;

        internal string Confirm(bool value) => Path(value).Confirm;

        internal string Create(bool value) => Path(value).Create;

        internal bool Custom(bool value) => value || scalar.Empty.Length > 0;

        internal string Slot(bool value) => Path(value).AccountSlot;

        internal string CategorySlot(bool value) => Path(value).CategorySlot;

        private WorkspacePath Path(bool value) => value ? income : expense;
    }

    private sealed class WorkspacePath
    {
        internal WorkspacePath(string[] states, string accountSlot, string categorySlot)
        {
            States = states;
            AccountSlot = accountSlot;
            CategorySlot = categorySlot;
        }

        private string[] States { get; }

        internal string Account => States[0];

        internal string AccountSlot { get; }

        internal string Amount => States[1];

        internal string Source => States[2];

        internal string Category => States[3];

        internal string CategorySlot { get; }

        internal string Confirm => States[4];

        internal string Create => States[5];
    }

    private sealed class WorkspaceScalar
    {
        internal WorkspaceScalar()
        {
            Empty = string.Empty;
            Utc = TimeSpan.Zero;
            Comparison = StringComparison.Ordinal;
            Styles = NumberStyles.None;
        }

        internal StringComparison Comparison { get; }

        internal string Empty { get; }

        internal NumberStyles Styles { get; }

        internal TimeSpan Utc { get; }
    }
}
