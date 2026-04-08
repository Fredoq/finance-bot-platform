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

    internal WorkspaceMove Move(string state, WorkspaceData data, WorkspaceInputRequestedCommand command, DateTimeOffset when, string timeZone)
    {
        string kind = command.Kind.Trim();
        return kind switch
        {
            "action" => Action(state, data, command.Value, when, timeZone),
            "text" => Text(state, data, command.Value),
            _ => new WorkspaceMove(state, body.Model(data, status: new StatusData("Input kind is not supported", data.Status.Notice)), null, string.Empty, null)
        };
    }

    private WorkspaceMove Action(string state, WorkspaceData data, string value, DateTimeOffset when, string timeZone)
    {
        string code = value.Trim();
        WorkspaceMove? guard = Guard(state, data, code, when);
        if (guard is not null)
        {
            return guard;
        }
        return Draft(state, data, code, when, timeZone)
            ?? Recent(state, data, code)
            ?? Report(state, data, code, when)
            ?? new WorkspaceMove(state, body.Model(data, status: new StatusData("This action is not available", string.Empty)), null, string.Empty, null);
    }

    private WorkspaceMove? Guard(string state, WorkspaceData data, string code, DateTimeOffset when) => Cancel(state, data, code) ?? Screen(state, data, code, when);

    private WorkspaceMove? Cancel(string state, WorkspaceData data, string code) => code switch
    {
        WorkspaceBody.AccountCancel when body.AccountState(state) => new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Account creation was cancelled"), null, string.Empty, null),
        WorkspaceBody.ExpenseCancel when body.ExpenseState(state) => new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Expense creation was cancelled"), null, string.Empty, null),
        WorkspaceBody.IncomeCancel when body.IncomeState(state) => new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Income creation was cancelled"), null, string.Empty, null),
        WorkspaceBody.TimeZoneCancel when body.TimeZoneScreen(state) => new WorkspaceMove(WorkspaceBody.HomeState, body.Reset(data, "Time zone update was cancelled"), null, string.Empty, null),
        _ => null
    };

    private WorkspaceMove? Screen(string state, WorkspaceData data, string code, DateTimeOffset when) => code switch
    {
        WorkspaceBody.RecentBack when body.RecentState(state) => recent.Return(data, state),
        WorkspaceBody.ShowBreakdown when body.SummaryScreen(state) => breakdown.Open(data),
        WorkspaceBody.SummaryBack when body.SummaryScreen(state) => summary.Action(data, code, when),
        WorkspaceBody.BreakdownBack when body.BreakdownScreen(state) => breakdown.Action(data, code, when),
        _ => null
    };

    private WorkspaceMove? Draft(string state, WorkspaceData data, string code, DateTimeOffset when, string timeZone) => state switch
    {
        WorkspaceBody.HomeState => draft.Home(data, code, when, timeZone),
        WorkspaceBody.CurrencyState => draft.Currency(data, code),
        WorkspaceBody.ConfirmState => draft.Confirm(data, code),
        WorkspaceBody.ExpenseAccountState => draft.Account(data, code, false),
        WorkspaceBody.ExpenseCategoryState => draft.Category(data, code, false),
        WorkspaceBody.ExpenseConfirmState => draft.Finish(data, code, false),
        WorkspaceBody.IncomeAccountState => draft.Account(data, code, true),
        WorkspaceBody.IncomeCategoryState => draft.Category(data, code, true),
        WorkspaceBody.IncomeConfirmState => draft.Finish(data, code, true),
        _ => null
    };

    private WorkspaceMove? Recent(string state, WorkspaceData data, string code) => state switch
    {
        WorkspaceBody.RecentListState => recent.List(data, code),
        WorkspaceBody.RecentDetailState => recent.Detail(data, code),
        WorkspaceBody.RecentDeleteState => recent.Delete(data, code),
        WorkspaceBody.RecentCategoryState => recent.Category(data, code),
        WorkspaceBody.RecentRecategorizeState => recent.Confirm(data, code),
        _ => null
    };

    private WorkspaceMove? Report(string state, WorkspaceData data, string code, DateTimeOffset when) => state switch
    {
        WorkspaceBody.SummaryState => summary.Action(data, code, when),
        WorkspaceBody.BreakdownState => breakdown.Action(data, code, when),
        _ => null
    };

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
        WorkspaceBody.ExpenseSourceState => draft.Source(data, value, false),
        WorkspaceBody.ExpenseCategoryState => draft.Text(data, value, false),
        WorkspaceBody.ExpenseConfirmState => ExpenseConfirm(data),
        WorkspaceBody.IncomeAccountState => new WorkspaceMove(WorkspaceBody.IncomeAccountState, body.Model(data, new FinancialData(), data.Choices, new StatusData("Use the buttons to choose one account or cancel", string.Empty)), null, string.Empty, null),
        WorkspaceBody.IncomeAmountState => draft.Total(data, value, true),
        WorkspaceBody.IncomeSourceState => draft.Source(data, value, true),
        WorkspaceBody.IncomeCategoryState => draft.Text(data, value, true),
        WorkspaceBody.IncomeConfirmState => IncomeConfirm(data),
        WorkspaceBody.RecentCategoryState => recent.Text(data, value),
        WorkspaceBody.RecentListState => new WorkspaceMove(WorkspaceBody.RecentListState, body.Model(data, status: new StatusData("Use the buttons to choose one transaction or go back", string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentDetailState => new WorkspaceMove(WorkspaceBody.RecentDetailState, body.Model(data, status: new StatusData("Use the buttons to continue", string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentDeleteState => new WorkspaceMove(WorkspaceBody.RecentDeleteState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)), null, string.Empty, null),
        WorkspaceBody.RecentRecategorizeState => new WorkspaceMove(WorkspaceBody.RecentRecategorizeState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)), null, string.Empty, null),
        WorkspaceBody.SummaryState => summary.Text(data),
        WorkspaceBody.BreakdownState => breakdown.Text(data),
        WorkspaceBody.TimeZoneState => draft.TimeZone(data, value),
        _ => new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, data.Accounts.Count == 0 ? WorkspaceBody.AddAccountPrompt : WorkspaceBody.ChooseActionPrompt), null, string.Empty, null)
    };

    private WorkspaceMove ExpenseConfirm(WorkspaceData data)
    {
        if (string.IsNullOrWhiteSpace(body.Value(data, false)))
        {
            return Source(data, false);
        }
        WorkspaceData transaction = body.Transaction(data, body.Pick(data, false), body.Category(data, false), body.Total(data, false), false);
        WorkspaceData sourced = body.Source(transaction, body.Value(data, false), false);
        WorkspaceData model = body.Model(sourced, choices: new ChoicesData(), status: new StatusData("Use the buttons to confirm or cancel", string.Empty));
        return new WorkspaceMove(WorkspaceBody.ExpenseConfirmState, model, null, string.Empty, null);
    }

    private WorkspaceMove IncomeConfirm(WorkspaceData data)
    {
        if (string.IsNullOrWhiteSpace(body.Value(data, true)))
        {
            return Source(data, true);
        }
        WorkspaceData transaction = body.Transaction(data, body.Pick(data, true), body.Category(data, true), body.Total(data, true), true);
        WorkspaceData sourced = body.Source(transaction, body.Value(data, true), true);
        WorkspaceData model = body.Model(sourced, choices: new ChoicesData(), status: new StatusData("Use the buttons to confirm or cancel", string.Empty));
        return new WorkspaceMove(WorkspaceBody.IncomeConfirmState, model, null, string.Empty, null);
    }

    private WorkspaceMove Source(WorkspaceData data, bool income)
    {
        WorkspaceData transaction = body.Transaction(data, body.Pick(data, income), new PickData(), body.Total(data, income), income);
        WorkspaceData sourced = body.Source(transaction, string.Empty, income);
        WorkspaceData model = body.Model(sourced, choices: new ChoicesData(), status: new StatusData("Merchant or description is required", string.Empty));
        return new WorkspaceMove(body.SourceCode(income), model, null, string.Empty, null);
    }
}
