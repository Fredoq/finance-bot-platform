namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceDraft
{
    private readonly WorkspaceBody body;
    private readonly WorkspaceAmount amount;

    internal WorkspaceDraft(WorkspaceBody body, WorkspaceAmount amount)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.amount = amount ?? throw new ArgumentNullException(nameof(amount));
    }

    internal WorkspaceMove Home(WorkspaceData data, string code, DateTimeOffset when)
    {
        if (code == WorkspaceBody.AddAccount)
        {
            return Move(WorkspaceBody.NameState, body.Account(data, new FinancialData(string.Empty, string.Empty, null)));
        }
        if (code == WorkspaceBody.AddExpense)
        {
            return Start(data, false);
        }
        if (code == WorkspaceBody.AddIncome)
        {
            return Start(data, true);
        }
        if (code == WorkspaceBody.ShowRecent)
        {
            return Move(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()));
        }
        if (code == WorkspaceBody.ShowSummary)
        {
            return Move(WorkspaceBody.SummaryState, body.Summary(data, WorkspaceBody.Month(when)));
        }
        return Move(WorkspaceBody.HomeState, body.Home(data.Accounts, data.Accounts.Count == 0 ? WorkspaceBody.AddAccountPrompt : WorkspaceBody.ChooseActionPrompt));
    }

    internal WorkspaceMove Currency(WorkspaceData data, string code) => code switch
    {
        WorkspaceBody.Rub => Code(data, "RUB"),
        WorkspaceBody.Usd => Code(data, "USD"),
        WorkspaceBody.Eur => Code(data, "EUR"),
        WorkspaceBody.Other => Move(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, string.Empty, data.Financial.Amount), custom: true)),
        _ => Move(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Choose one currency option or send a 3 letter code", string.Empty), data.Custom))
    };

    internal WorkspaceMove Confirm(WorkspaceData data, string code) => code == WorkspaceBody.CreateAccountCode ? Create(data) : Move(WorkspaceBody.ConfirmState, body.Account(data, data.Financial, new StatusData("Confirm the account or cancel", string.Empty)));

    internal WorkspaceMove Account(WorkspaceData data, string code, bool income)
    {
        int slot = body.Slot(code, body.AccountSlot(income));
        OptionData item = body.Option(data.Choices.Accounts, slot);
        return Move(body.AmountCode(income), body.Transaction(data, new PickData(item.Id, item.Name, item.Note), new PickData(), null, income));
    }

    internal WorkspaceMove Category(WorkspaceData data, string code, bool income)
    {
        int slot = body.Slot(code, body.CategorySlot(income));
        OptionData item = body.Option(data.Choices.Categories, slot);
        if (string.IsNullOrWhiteSpace(body.Value(data, income)))
        {
            return Source(data, income);
        }
        WorkspaceData state = body.Source(body.Transaction(data, body.Pick(data, income), new PickData(item.Id, item.Name, item.Note), body.Total(data, income), income), body.Value(data, income), income);
        return Move(body.ConfirmCode(income), state);
    }

    internal WorkspaceMove Finish(WorkspaceData data, string code, bool income)
    {
        if (code == body.CreateCode(income))
        {
            return Record(data, income);
        }
        if (string.IsNullOrWhiteSpace(body.Value(data, income)))
        {
            return Source(data, income);
        }
        string text = income ? "Confirm the income or cancel" : "Confirm the expense or cancel";
        WorkspaceData item = body.Source(body.Transaction(data, body.Pick(data, income), body.Category(data, income), body.Total(data, income), income), body.Value(data, income), income);
        return Move(body.ConfirmCode(income), body.Model(item, choices: new ChoicesData(), status: new StatusData(text, string.Empty)));
    }

    internal WorkspaceMove Name(WorkspaceData data, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Move(WorkspaceBody.NameState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Account name is required", string.Empty)));
        }
        if (data.Financial.Amount.HasValue && !string.IsNullOrWhiteSpace(data.Financial.Currency))
        {
            return Move(WorkspaceBody.ConfirmState, body.Account(data, new FinancialData(text, data.Financial.Currency, data.Financial.Amount)));
        }
        if (!string.IsNullOrWhiteSpace(data.Financial.Currency))
        {
            return Move(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(text, data.Financial.Currency, data.Financial.Amount)));
        }
        return Move(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(text, string.Empty, data.Financial.Amount)));
    }

    internal WorkspaceMove Code(WorkspaceData data, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string text = value.Trim().ToUpperInvariant();
        bool valid = text.Length == 3 && text.All(item => item is >= 'A' and <= 'Z');
        if (!valid)
        {
            return Move(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Currency code must contain 3 letters", string.Empty), true));
        }
        return data.Financial.Amount.HasValue
            ? Move(WorkspaceBody.ConfirmState, body.Account(data, new FinancialData(data.Financial.Name, text, data.Financial.Amount)))
            : Move(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(data.Financial.Name, text, data.Financial.Amount)));
    }

    internal WorkspaceMove Balance(WorkspaceData data, string value)
    {
        bool ok = amount.Try(value, out decimal total);
        return ok
            ? Move(WorkspaceBody.ConfirmState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, total)))
            : Move(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Balance must be a number", string.Empty)));
    }

    internal WorkspaceMove Total(WorkspaceData data, string value, bool income)
    {
        bool ok = amount.Try(value, out decimal total);
        if (!ok)
        {
            return Move(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income, new ChoicesData(), new StatusData("Enter a valid numeric amount", string.Empty)));
        }
        if (amount.Scale(total) > 4)
        {
            return Move(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income, new ChoicesData(), new StatusData("Enter up to 4 decimal places", string.Empty)));
        }
        if (total <= 0m)
        {
            return Move(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income, new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty)));
        }
        return Move(body.SourceCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), total, income));
    }

    internal WorkspaceMove Source(WorkspaceData data, string value, bool income)
    {
        ArgumentNullException.ThrowIfNull(value);
        string text = value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? Move(body.SourceCode(income), body.Model(body.Source(body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income), string.Empty, income), choices: new ChoicesData(), status: new StatusData("Merchant or description is required", string.Empty)))
            : Move(body.SourceCode(income), body.Source(body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income), text, income), text);
    }

    internal WorkspaceMove Text(WorkspaceData data, string value, bool income)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (string.IsNullOrWhiteSpace(body.Value(data, income)))
        {
            return Source(data, income);
        }
        string text = value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? Move(body.CategoryCode(income), body.Model(body.Source(body.Transaction(data, body.Pick(data, income), body.Category(data, income), body.Total(data, income), income), body.Value(data, income), income), choices: data.Choices, status: new StatusData("Category name is required", string.Empty)))
            : Move(body.CategoryCode(income), body.Model(body.Source(body.Transaction(data, body.Pick(data, income), body.Category(data, income), body.Total(data, income), income), body.Value(data, income), income), choices: data.Choices, status: new StatusData()), text);
    }

    private WorkspaceMove Start(WorkspaceData data, bool income)
    {
        if (data.Accounts.Count == 0)
        {
            return Move(WorkspaceBody.HomeState, body.Home(data.Accounts, income ? "Add an account to record an income" : "Add an account to record an expense"));
        }
        if (data.Accounts.Count == 1)
        {
            AccountData item = data.Accounts[0];
            return Move(body.AmountCode(income), body.Transaction(data, new PickData(item.Id, item.Name, item.Currency), new PickData(), null, income));
        }
        return Move(body.AccountCode(income), body.Transaction(data, new PickData(), new PickData(), null, income, new ChoicesData(body.Accounts(data.Accounts), [])));
    }

    private WorkspaceMove Create(WorkspaceData data)
    {
        if (string.IsNullOrWhiteSpace(data.Financial.Name))
        {
            return Move(WorkspaceBody.NameState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Account name is required", string.Empty)));
        }
        if (string.IsNullOrWhiteSpace(data.Financial.Currency))
        {
            return Move(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Currency code must contain 3 letters", string.Empty), true));
        }
        if (!data.Financial.Amount.HasValue)
        {
            return Move(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Balance must be a number", string.Empty)));
        }
        return Move(WorkspaceBody.HomeState, body.Reset(data, "Account was created"), new AccountDraft(data.Financial.Name, data.Financial.Currency, data.Financial.Amount.Value));
    }

    private WorkspaceMove Record(WorkspaceData data, bool income)
    {
        string accountId = body.Resolve(data, income);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Start(data, income);
        }
        decimal? total = body.Total(data, income);
        if (!total.HasValue || total.Value <= 0m)
        {
            return Move(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), total, income, new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty)));
        }
        string source = body.Value(data, income).Trim();
        if (string.IsNullOrWhiteSpace(source))
        {
            WorkspaceData state = body.Source(body.Transaction(data, body.Pick(data, income), new PickData(), total, income), string.Empty, income);
            return Move(body.SourceCode(income), body.Model(state, choices: new ChoicesData(), status: new StatusData("Merchant or description is required", string.Empty)));
        }
        PickData category = body.Category(data, income);
        if (string.IsNullOrWhiteSpace(category.Id))
        {
            WorkspaceData state = body.Source(body.Transaction(data, body.Pick(data, income), new PickData(), total, income), source, income);
            return Move(body.CategoryCode(income), body.Model(state, choices: new ChoicesData(), status: new StatusData("Choose one category or send a new name", string.Empty)));
        }
        PickData account = body.Pick(data, income);
        WorkspaceData item = body.Source(body.Transaction(data, new PickData(accountId, account.Name, account.Note), category, total, income), source, income);
        return Move(body.ConfirmCode(income), item, new TransactionNote(accountId, category.Id, total.Value, body.Kind(income), source));
    }

    private WorkspaceMove Source(WorkspaceData data, bool income)
    {
        WorkspaceData state = body.Source(body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income), string.Empty, income);
        return Move(body.SourceCode(income), body.Model(state, choices: new ChoicesData(), status: new StatusData("Merchant or description is required", string.Empty)));
    }

    private static WorkspaceMove Move(string code, WorkspaceData data) => new(code, data);

    private static WorkspaceMove Move(string code, WorkspaceData data, AccountDraft note) => new(code, data, note);

    private static WorkspaceMove Move(string code, WorkspaceData data, string category) => new(code, data, category);

    private static WorkspaceMove Move(string code, WorkspaceData data, TransactionNote note) => new(code, data, note);
}
