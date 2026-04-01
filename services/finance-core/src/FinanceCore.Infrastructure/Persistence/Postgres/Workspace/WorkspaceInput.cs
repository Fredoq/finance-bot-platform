using Finance.Application.Contracts.Entry;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceInput
{
    private readonly WorkspaceBody body;
    private readonly WorkspaceDraft draft;
    private readonly WorkspaceRecent recent;
    private readonly WorkspaceSummary summary;
    private readonly WorkspaceBreakdown breakdown;

    internal WorkspaceInput(WorkspaceBody body, WorkspaceDraft draft, WorkspaceRecent recent, WorkspaceSummary summary, WorkspaceBreakdown breakdown)
    {
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.draft = draft ?? throw new ArgumentNullException(nameof(draft));
        this.recent = recent ?? throw new ArgumentNullException(nameof(recent));
        this.summary = summary ?? throw new ArgumentNullException(nameof(summary));
        this.breakdown = breakdown ?? throw new ArgumentNullException(nameof(breakdown));
    }

    internal WorkspaceMove Move(string state, WorkspaceData data, WorkspaceInputRequestedCommand command, DateTimeOffset when)
    {
        string kind = command.Kind.Trim();
        return kind switch
        {
            "action" => Action(state, data, command.Value, when),
            "text" => Text(state, data, command.Value),
            _ => new WorkspaceMove(state, body.Model(data, status: new StatusData("Input kind is not supported", data.Status.Notice)), null, string.Empty, null)
        };
    }

    private WorkspaceMove Action(string state, WorkspaceData data, string value, DateTimeOffset when)
    {
        string code = value.Trim();
        if (code == WorkspaceBody.AccountCancel && body.AccountState(state))
        {
            return new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Account creation was cancelled"), null, string.Empty, null);
        }
        if (code == WorkspaceBody.ExpenseCancel && body.ExpenseState(state))
        {
            return new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Expense creation was cancelled"), null, string.Empty, null);
        }
        if (code == WorkspaceBody.IncomeCancel && body.IncomeState(state))
        {
            return new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Income creation was cancelled"), null, string.Empty, null);
        }
        if (code == WorkspaceBody.RecentBack && body.RecentState(state))
        {
            return recent.Return(data, state);
        }
        if (code == WorkspaceBody.ShowBreakdown && body.SummaryScreen(state))
        {
            return breakdown.Open(data);
        }
        if (code == WorkspaceBody.SummaryBack && body.SummaryScreen(state))
        {
            return summary.Action(data, code, when);
        }
        if (code == WorkspaceBody.BreakdownBack && body.BreakdownScreen(state))
        {
            return breakdown.Action(data, code, when);
        }
        return state switch
        {
            WorkspaceBody.HomeState => draft.Home(data, code, when),
            WorkspaceBody.CurrencyState => draft.Currency(data, code),
            WorkspaceBody.ConfirmState => draft.Confirm(data, code),
            WorkspaceBody.ExpenseAccountState => draft.Account(data, code, false),
            WorkspaceBody.ExpenseCategoryState => draft.Category(data, code, false),
            WorkspaceBody.ExpenseConfirmState => draft.Finish(data, code, false),
            WorkspaceBody.IncomeAccountState => draft.Account(data, code, true),
            WorkspaceBody.IncomeCategoryState => draft.Category(data, code, true),
            WorkspaceBody.IncomeConfirmState => draft.Finish(data, code, true),
            WorkspaceBody.RecentListState => recent.List(data, code),
            WorkspaceBody.RecentDetailState => recent.Detail(data, code),
            WorkspaceBody.RecentDeleteState => recent.Delete(data, code),
            WorkspaceBody.RecentCategoryState => recent.Category(data, code),
            WorkspaceBody.RecentRecategorizeState => recent.Confirm(data, code),
            WorkspaceBody.SummaryState => summary.Action(data, code, when),
            WorkspaceBody.BreakdownState => breakdown.Action(data, code, when),
            _ => new WorkspaceMove(state, body.Model(data, status: new StatusData("This action is not available", string.Empty)), null, string.Empty, null)
        };
    }

    private WorkspaceMove Text(string state, WorkspaceData data, string value) => state switch
    {
        WorkspaceBody.HomeState when data.Accounts.Count == 0 => new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, WorkspaceBody.AddAccountPrompt), null, string.Empty, null),
        WorkspaceBody.HomeState => new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, WorkspaceBody.ChooseActionPrompt), null, string.Empty, null),
        WorkspaceBody.NameState => draft.Name(data, value),
        WorkspaceBody.CurrencyState => draft.Code(data, value),
        WorkspaceBody.BalanceState => draft.Balance(data, value),
        WorkspaceBody.ConfirmState => new WorkspaceMove(WorkspaceBody.ConfirmState, body.Account(data, data.Financial, new StatusData("Use the buttons to confirm or cancel", string.Empty)), null, string.Empty, null),
        WorkspaceBody.ExpenseAccountState => new WorkspaceMove(WorkspaceBody.ExpenseAccountState, body.Model(data, new FinancialData(), data.Choices, new StatusData("Use the buttons to choose one account or cancel", string.Empty)), null, string.Empty, null),
        WorkspaceBody.ExpenseAmountState => draft.Total(data, value, false),
        WorkspaceBody.ExpenseCategoryState => draft.Text(data, value, false),
        WorkspaceBody.ExpenseConfirmState => new WorkspaceMove(WorkspaceBody.ExpenseConfirmState, body.Transaction(data, body.Pick(data, false), body.Category(data, false), body.Total(data, false), false, new ChoicesData(), new StatusData("Use the buttons to confirm or cancel", string.Empty)), null, string.Empty, null),
        WorkspaceBody.IncomeAccountState => new WorkspaceMove(WorkspaceBody.IncomeAccountState, body.Model(data, new FinancialData(), data.Choices, new StatusData("Use the buttons to choose one account or cancel", string.Empty)), null, string.Empty, null),
        WorkspaceBody.IncomeAmountState => draft.Total(data, value, true),
        WorkspaceBody.IncomeCategoryState => draft.Text(data, value, true),
        WorkspaceBody.IncomeConfirmState => new WorkspaceMove(WorkspaceBody.IncomeConfirmState, body.Transaction(data, body.Pick(data, true), body.Category(data, true), body.Total(data, true), true, new ChoicesData(), new StatusData("Use the buttons to confirm or cancel", string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentCategoryState => recent.Text(data, value),
        WorkspaceBody.RecentListState => new WorkspaceMove(WorkspaceBody.RecentListState, body.Model(data, status: new StatusData("Use the buttons to choose one transaction or go back", string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentDetailState => new WorkspaceMove(WorkspaceBody.RecentDetailState, body.Model(data, status: new StatusData("Use the buttons to continue", string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentDeleteState => new WorkspaceMove(WorkspaceBody.RecentDeleteState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentRecategorizeState => new WorkspaceMove(WorkspaceBody.RecentRecategorizeState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)), null, string.Empty, null),
        WorkspaceBody.SummaryState => summary.Text(data),
        WorkspaceBody.BreakdownState => breakdown.Text(data),
        _ => new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, data.Accounts.Count == 0 ? WorkspaceBody.AddAccountPrompt : WorkspaceBody.ChooseActionPrompt), null, string.Empty, null)
    };
}
