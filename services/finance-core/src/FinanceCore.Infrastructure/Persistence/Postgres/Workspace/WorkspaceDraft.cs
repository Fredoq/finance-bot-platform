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

    internal WorkspaceMove Home(WorkspaceData data, string code)
    {
        if (code == WorkspaceBody.AddAccount)
        {
            return new WorkspaceMove(WorkspaceBody.NameState, body.Account(data, new FinancialData(string.Empty, string.Empty, null)), null, string.Empty, null);
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
            return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()), null, string.Empty, null);
        }
        return new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, data.Accounts.Count == 0 ? WorkspaceBody.AddAccountPrompt : WorkspaceBody.ChooseActionPrompt), null, string.Empty, null);
    }

    internal WorkspaceMove Currency(WorkspaceData data, string code) => code switch
    {
        WorkspaceBody.Rub => Code(data, "RUB"),
        WorkspaceBody.Usd => Code(data, "USD"),
        WorkspaceBody.Eur => Code(data, "EUR"),
        WorkspaceBody.Other => new WorkspaceMove(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, string.Empty, data.Financial.Amount), custom: true), null, string.Empty, null),
        _ => new WorkspaceMove(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Choose one currency option or send a 3 letter code", string.Empty), data.Custom), null, string.Empty, null)
    };

    internal WorkspaceMove Confirm(WorkspaceData data, string code) => code == WorkspaceBody.CreateAccountCode ? Create(data) : new WorkspaceMove(WorkspaceBody.ConfirmState, body.Account(data, data.Financial, new StatusData("Confirm the account or cancel", string.Empty)), null, string.Empty, null);

    internal WorkspaceMove Account(WorkspaceData data, string code, bool income)
    {
        int slot = body.Slot(code, body.AccountSlot(income));
        OptionData item = body.Option(data.Choices.Accounts, slot);
        return new WorkspaceMove(body.AmountCode(income), body.Transaction(data, new PickData(item.Id, item.Name, item.Note), new PickData(), null, income), null, string.Empty, null);
    }

    internal WorkspaceMove Category(WorkspaceData data, string code, bool income)
    {
        int slot = body.Slot(code, body.CategorySlot(income));
        OptionData item = body.Option(data.Choices.Categories, slot);
        return new WorkspaceMove(body.ConfirmCode(income), body.Transaction(data, body.Pick(data, income), new PickData(item.Id, item.Name, item.Note), body.Total(data, income), income), null, string.Empty, null);
    }

    internal WorkspaceMove Finish(WorkspaceData data, string code, bool income)
    {
        if (code == body.CreateCode(income))
        {
            return Record(data, income);
        }
        string text = income ? "Confirm the income or cancel" : "Confirm the expense or cancel";
        return new WorkspaceMove(body.ConfirmCode(income), body.Transaction(data, body.Pick(data, income), body.Category(data, income), body.Total(data, income), income, new ChoicesData(), new StatusData(text, string.Empty)), null, string.Empty, null);
    }

    internal WorkspaceMove Name(WorkspaceData data, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WorkspaceMove(WorkspaceBody.NameState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Account name is required", string.Empty)), null, string.Empty, null);
        }
        if (data.Financial.Amount.HasValue && !string.IsNullOrWhiteSpace(data.Financial.Currency))
        {
            return new WorkspaceMove(WorkspaceBody.ConfirmState, body.Account(data, new FinancialData(text, data.Financial.Currency, data.Financial.Amount)), null, string.Empty, null);
        }
        if (!string.IsNullOrWhiteSpace(data.Financial.Currency))
        {
            return new WorkspaceMove(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(text, data.Financial.Currency, data.Financial.Amount)), null, string.Empty, null);
        }
        return new WorkspaceMove(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(text, string.Empty, data.Financial.Amount)), null, string.Empty, null);
    }

    internal WorkspaceMove Code(WorkspaceData data, string value)
    {
        string text = value.Trim().ToUpperInvariant();
        bool valid = text.Length == 3 && text.All(char.IsLetter);
        if (!valid)
        {
            return new WorkspaceMove(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Currency code must contain 3 letters", string.Empty), true), null, string.Empty, null);
        }
        return data.Financial.Amount.HasValue
            ? new WorkspaceMove(WorkspaceBody.ConfirmState, body.Account(data, new FinancialData(data.Financial.Name, text, data.Financial.Amount)), null, string.Empty, null)
            : new WorkspaceMove(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(data.Financial.Name, text, data.Financial.Amount)), null, string.Empty, null);
    }

    internal WorkspaceMove Balance(WorkspaceData data, string value)
    {
        bool ok = amount.Try(value, out decimal total);
        return ok
            ? new WorkspaceMove(WorkspaceBody.ConfirmState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, total)), null, string.Empty, null)
            : new WorkspaceMove(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Balance must be a number", string.Empty)), null, string.Empty, null);
    }

    internal WorkspaceMove Total(WorkspaceData data, string value, bool income)
    {
        bool ok = amount.Try(value, out decimal total);
        if (!ok)
        {
            return new WorkspaceMove(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income, new ChoicesData(), new StatusData("Enter a valid numeric amount", string.Empty)), null, string.Empty, null);
        }
        if (amount.Scale(total) > 4)
        {
            return new WorkspaceMove(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income, new ChoicesData(), new StatusData("Enter up to 4 decimal places", string.Empty)), null, string.Empty, null);
        }
        if (total <= 0m)
        {
            return new WorkspaceMove(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income, new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty)), null, string.Empty, null);
        }
        return new WorkspaceMove(body.CategoryCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), total, income), null, string.Empty, null);
    }

    internal WorkspaceMove Text(WorkspaceData data, string value, bool income)
    {
        string text = value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? new WorkspaceMove(body.CategoryCode(income), body.Model(data, new FinancialData(), data.Choices, new StatusData("Category name is required", string.Empty)), null, string.Empty, null)
            : new WorkspaceMove(body.CategoryCode(income), body.Model(data, new FinancialData(), data.Choices, new StatusData()), null, text, null);
    }

    private WorkspaceMove Start(WorkspaceData data, bool income)
    {
        if (data.Accounts.Count == 0)
        {
            return new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, income ? "Add an account to record an income" : "Add an account to record an expense"), null, string.Empty, null);
        }
        if (data.Accounts.Count == 1)
        {
            AccountData item = data.Accounts[0];
            return new WorkspaceMove(body.AmountCode(income), body.Transaction(data, new PickData(item.Id, item.Name, item.Currency), new PickData(), null, income), null, string.Empty, null);
        }
        return new WorkspaceMove(body.AccountCode(income), body.Transaction(data, new PickData(), new PickData(), null, income, new ChoicesData(body.Accounts(data.Accounts), [])), null, string.Empty, null);
    }

    private WorkspaceMove Create(WorkspaceData data)
    {
        if (string.IsNullOrWhiteSpace(data.Financial.Name))
        {
            return new WorkspaceMove(WorkspaceBody.NameState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Account name is required", string.Empty)), null, string.Empty, null);
        }
        if (string.IsNullOrWhiteSpace(data.Financial.Currency))
        {
            return new WorkspaceMove(WorkspaceBody.CurrencyState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Currency code must contain 3 letters", string.Empty), true), null, string.Empty, null);
        }
        if (!data.Financial.Amount.HasValue)
        {
            return new WorkspaceMove(WorkspaceBody.BalanceState, body.Account(data, new FinancialData(data.Financial.Name, data.Financial.Currency, data.Financial.Amount), new StatusData("Balance must be a number", string.Empty)), null, string.Empty, null);
        }
        return new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Account was created"), new AccountDraft(data.Financial.Name, data.Financial.Currency, data.Financial.Amount.Value), string.Empty, null);
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
            return new WorkspaceMove(body.AmountCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), total, income, new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty)), null, string.Empty, null);
        }
        PickData category = body.Category(data, income);
        if (string.IsNullOrWhiteSpace(category.Id))
        {
            return new WorkspaceMove(body.CategoryCode(income), body.Transaction(data, body.Pick(data, income), new PickData(), total, income, new ChoicesData(), new StatusData("Choose one category or send a new name", string.Empty)), null, string.Empty, null);
        }
        PickData account = body.Pick(data, income);
        WorkspaceData item = body.Transaction(data, new PickData(accountId, account.Name, account.Note), category, total, income);
        return new WorkspaceMove(body.ConfirmCode(income), item, null, string.Empty, new TransactionNote(accountId, category.Id, total.Value, body.Kind(income)));
    }
}
