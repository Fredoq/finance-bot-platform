using System.Text;

namespace TelegramGateway.Application.Entry.Workspace.Slices;

internal sealed class WorkspaceText
{
    private readonly WorkspaceHtml html;

    internal WorkspaceText(WorkspaceHtml html) => this.html = html ?? throw new ArgumentNullException(nameof(html));

    internal string Text(string state, bool fresh, WorkspaceData data) => state switch
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

    private string Home(bool fresh, WorkspaceData data)
    {
        var text = new StringBuilder();
        if (data.Accounts.Count == 0)
        {
            text.AppendLine("<b>Finance workspace</b>");
            if (!string.IsNullOrWhiteSpace(data.Status.Notice))
            {
                text.AppendLine(WorkspaceHtml.Escape(data.Status.Notice));
            }
            text.Append(fresh ? "Add your first account to start tracking your balance" : "Add an account to start tracking your balance");
            return text.ToString().TrimEnd();
        }
        text.AppendLine("<b>Your accounts</b>");
        foreach (AccountData item in data.Accounts)
        {
            text.AppendLine($"- <b>{WorkspaceHtml.Escape(item.Name)}</b>: {html.Amount(item.Amount, item.Currency)}");
        }
        if (!string.IsNullOrWhiteSpace(data.Status.Notice))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Notice));
        }
        text.Append("Choose the next action");
        return text.ToString().TrimEnd();
    }

    private static string Name(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
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
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.AppendLine($"Name: <b>{WorkspaceHtml.Escape(data.Financial.Name)}</b>");
        text.Append(data.Custom ? "Send a 3 letter currency code" : "Choose the account currency");
        return text.ToString().TrimEnd();
    }

    private static string Balance(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
        }
        text.AppendLine("<b>New account</b>");
        text.AppendLine($"Name: <b>{WorkspaceHtml.Escape(data.Financial.Name)}</b>");
        text.AppendLine($"Currency: {WorkspaceHtml.Code(data.Financial.Currency)}");
        text.Append("Send the current balance");
        return text.ToString().TrimEnd();
    }

    private string Confirm(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
        }
        text.AppendLine("<b>Confirm account</b>");
        text.AppendLine($"Name: <b>{WorkspaceHtml.Escape(data.Financial.Name)}</b>");
        text.AppendLine($"Currency: {WorkspaceHtml.Code(data.Financial.Currency)}");
        text.Append($"Balance: <b>{html.Amount(data.Financial.Amount, data.Financial.Currency)}</b>");
        return text.ToString().TrimEnd();
    }

    private static string ExpenseAccount(WorkspaceData data) => Transaction("New expense", data, data.Expense, static (text, _) => text.Append("Choose the account"));

    private static string ExpenseAmount(WorkspaceData data) => Transaction("New expense", data, data.Expense, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Currency: {WorkspaceHtml.Code(item.Account.Note)}");
        text.Append("Send the amount");
    });

    private string ExpenseCategory(WorkspaceData data) => Transaction("New expense", data, data.Expense, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Amount: <b>{html.Amount(item.Amount, item.Account.Note)}</b>");
        text.Append("Choose the category or send a new name");
    });

    private string ExpenseConfirm(WorkspaceData data) => Transaction("Confirm expense", data, data.Expense, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))}</b>");
        text.Append($"Amount: <b>{html.Amount(item.Amount, item.Account.Note)}</b>");
    });

    private static string IncomeAccount(WorkspaceData data) => Transaction("New income", data, data.Income, static (text, _) => text.Append("Choose the account"));

    private static string IncomeAmount(WorkspaceData data) => Transaction("New income", data, data.Income, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Currency: {WorkspaceHtml.Code(item.Account.Note)}");
        text.Append("Send the amount");
    });

    private string IncomeCategory(WorkspaceData data) => Transaction("New income", data, data.Income, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Amount: <b>{html.Amount(item.Amount, item.Account.Note)}</b>");
        text.Append("Choose the category or send a new name");
    });

    private string IncomeConfirm(WorkspaceData data) => Transaction("Confirm income", data, data.Income, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))}</b>");
        text.Append($"Amount: <b>{html.Amount(item.Amount, item.Account.Note)}</b>");
    });

    private static string Transaction(string title, WorkspaceData data, TransactionData item, Action<StringBuilder, TransactionData> note)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
        }
        text.AppendLine($"<b>{title}</b>");
        note(text, item);
        return text.ToString().TrimEnd();
    }

    private string RecentList(WorkspaceData data)
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
        }
        text.AppendLine("<b>Recent transactions</b>");
        if (data.Recent.Items.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(data.Status.Notice))
            {
                text.AppendLine(WorkspaceHtml.Escape(data.Status.Notice));
            }
            text.Append("No transactions yet");
            return text.ToString().TrimEnd();
        }
        foreach (RecentItemData item in data.Recent.Items)
        {
            text.AppendLine($"{item.Slot}. {WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))} · {html.Amount(item.Amount, item.Currency)}");
            text.AppendLine($"   {WorkspaceHtml.Escape(item.Account.Name)} · {WorkspaceHtml.Escape(WorkspaceHtml.When(item.OccurredUtc))}");
        }
        if (!string.IsNullOrWhiteSpace(data.Status.Notice))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Notice));
        }
        text.Append($"Page {data.Recent.Page + 1}");
        return text.ToString().TrimEnd();
    }

    private string RecentDetail(WorkspaceData data) => Recent("Transaction", data, (text, item) =>
    {
        text.AppendLine($"Kind: <b>{WorkspaceHtml.Escape(WorkspaceHtml.Title(item.Kind))}</b>");
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))}</b>");
        text.AppendLine($"Amount: <b>{html.Amount(item.Amount, item.Currency)}</b>");
        text.Append($"Recorded: <code>{WorkspaceHtml.Escape(WorkspaceHtml.When(item.OccurredUtc))}</code>");
    });

    private string RecentDelete(WorkspaceData data) => Recent("Delete transaction", data, (text, item) =>
    {
        text.AppendLine($"Category: <b>{WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))}</b>");
        text.AppendLine($"Amount: <b>{html.Amount(item.Amount, item.Currency)}</b>");
        text.Append("Delete this transaction");
    });

    private string RecentCategory(WorkspaceData data) => Recent("Change category", data, (text, item) =>
    {
        text.AppendLine($"Current: <b>{WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))}</b>");
        text.AppendLine($"Amount: <b>{html.Amount(item.Amount, item.Currency)}</b>");
        text.Append("Choose the category or send a new name");
    });

    private string RecentRecategorize(WorkspaceData data) => Recent("Confirm category", data, (text, item) =>
    {
        text.AppendLine($"Account: <b>{WorkspaceHtml.Escape(item.Account.Name)}</b>");
        text.AppendLine($"Category: <b>{WorkspaceHtml.Escape(html.Category(item.Category.Name, item.Category.Note))}</b>");
        text.Append($"Amount: <b>{html.Amount(item.Amount, item.Currency)}</b>");
    });

    private static string Recent(string title, WorkspaceData data, Action<StringBuilder, RecentItemData> note)
    {
        RecentItemData item = WorkspaceBody.Selected(data);
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(data.Status.Error))
        {
            text.AppendLine(WorkspaceHtml.Escape(data.Status.Error));
        }
        text.AppendLine($"<b>{title}</b>");
        note(text, item);
        return text.ToString().TrimEnd();
    }
}
