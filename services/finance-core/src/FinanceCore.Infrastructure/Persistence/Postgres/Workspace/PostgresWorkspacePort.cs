using System.Globalization;
using System.Text.Json;
using Finance.Application.Contracts.Entry;
using Finance.Application.Contracts.Messaging;
using FinanceCore.Application.Workspace.Ports;
using FinanceCore.Domain.Workspace.Models;
using FinanceCore.Domain.Workspace.Policies;
using Npgsql;
using NpgsqlTypes;

namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class PostgresWorkspacePort : IWorkspacePort, IWorkspaceInputPort
{
    private const int RetryCount = 8;
    private const NumberStyles AmountStyles = NumberStyles.Number;
    private const string HomeState = "home";
    private const string NameState = "account.name";
    private const string CurrencyState = "account.currency";
    private const string BalanceState = "account.balance";
    private const string ConfirmState = "account.confirm";
    private const string ExpenseAccountState = "transaction.expense.account";
    private const string ExpenseAmountState = "transaction.expense.amount";
    private const string ExpenseCategoryState = "transaction.expense.category";
    private const string ExpenseConfirmState = "transaction.expense.confirm";
    private const string IncomeAccountState = "transaction.income.account";
    private const string IncomeAmountState = "transaction.income.amount";
    private const string IncomeCategoryState = "transaction.income.category";
    private const string IncomeConfirmState = "transaction.income.confirm";
    private const string RecentListState = "transaction.recent.list";
    private const string RecentDetailState = "transaction.recent.detail";
    private const string RecentDeleteState = "transaction.recent.delete.confirm";
    private const string RecentCategoryState = "transaction.recent.category";
    private const string RecentRecategorizeState = "transaction.recent.recategorize.confirm";
    private const string AddAccount = "account.add";
    private const string AddExpense = "transaction.expense.add";
    private const string AddIncome = "transaction.income.add";
    private const string ShowRecent = "transaction.recent.show";
    private const string AccountCancel = "account.cancel";
    private const string ExpenseCancel = "transaction.expense.cancel";
    private const string IncomeCancel = "transaction.income.cancel";
    private const string CreateAccountCode = "account.create";
    private const string CreateExpenseCode = "transaction.expense.create";
    private const string CreateIncomeCode = "transaction.income.create";
    private const string RecentBack = "transaction.recent.back";
    private const string RecentDelete = "transaction.recent.delete";
    private const string RecentDeleteApply = "transaction.recent.delete.apply";
    private const string RecentRecategorize = "transaction.recent.recategorize";
    private const string RecentRecategorizeApply = "transaction.recent.recategorize.apply";
    private const string RecentPrevious = "transaction.recent.page.prev";
    private const string RecentNext = "transaction.recent.page.next";
    private const string Rub = "account.currency.rub";
    private const string Usd = "account.currency.usd";
    private const string Eur = "account.currency.eur";
    private const string Other = "account.currency.other";
    private const string ExpenseAccountSlot = "transaction.expense.account.";
    private const string ExpenseCategorySlot = "transaction.expense.category.";
    private const string IncomeAccountSlot = "transaction.income.account.";
    private const string IncomeCategorySlot = "transaction.income.category.";
    private const string RecentItemSlot = "transaction.recent.item.";
    private const string RecentCategorySlot = "transaction.recent.category.";
    private const int RecentPageSize = 5;
    private const string ExpenseKind = "expense";
    private const string IncomeKind = "income";
    private const string DeleteMode = "delete";
    private const string RecategorizeMode = "recategorize";
    private const string ViewContract = "workspace.view.requested";
    private const string ViewSource = "finance-core";
    private const string CreatedUtc = "created_utc";
    private const string UpdatedUtc = "updated_utc";
    private const string UserId = "user_id";
    private const string AddAccountPrompt = "Tap Add account to start";
    private const string ChooseActionPrompt = "Choose one action";
    private const string ConfirmGoBackPrompt = "Use the buttons to confirm or go back";
    private const string TransactionMissingNotice = "Transaction was not found";
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource source;
    private readonly IWorkspaceActions policy;
    internal PostgresWorkspacePort(NpgsqlDataSource source, IWorkspaceActions policy)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }
    public async ValueTask Save(MessageEnvelope<WorkspaceRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        DateTimeOffset when = Utc(message.Payload.OccurredUtc, nameof(message));
        string raw = JsonSerializer.Serialize(message, json);
        await using NpgsqlConnection link = await source.OpenConnectionAsync(token);
        await using NpgsqlTransaction lane = await link.BeginTransactionAsync(token);
        bool fresh = await Inbox(link, lane, message, raw, token);
        if (!fresh)
        {
            await lane.CommitAsync(token);
            return;
        }
        (Guid userId, bool isNewUser) = await User(link, lane, message.Payload.Identity, message.Payload.Profile, when, token);
        WorkspaceWrite state = await Start(link, lane, userId, message.Payload, when, token);
        await Outbox(link, lane, message, state.State, new WorkspaceViewNote(message.Payload.Identity, message.Payload.Profile, isNewUser, state.IsNew, when), token);
        await Processed(link, lane, message, token);
        await lane.CommitAsync(token);
    }
    public async ValueTask Save(MessageEnvelope<WorkspaceInputRequestedCommand> message, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(message);
        DateTimeOffset when = Utc(message.Payload.OccurredUtc, nameof(message));
        string raw = JsonSerializer.Serialize(message, json);
        await using NpgsqlConnection link = await source.OpenConnectionAsync(token);
        await using NpgsqlTransaction lane = await link.BeginTransactionAsync(token);
        bool fresh = await Inbox(link, lane, message, raw, token);
        if (!fresh)
        {
            await lane.CommitAsync(token);
            return;
        }
        (Guid userId, bool isNewUser) = await User(link, lane, message.Payload.Identity, message.Payload.Profile, when, token);
        WorkspaceWrite state = await Input(link, lane, userId, message.Payload, when, token);
        await Outbox(link, lane, message, state.State, new WorkspaceViewNote(message.Payload.Identity, message.Payload.Profile, isNewUser, state.IsNew, when), token);
        await Processed(link, lane, message, token);
        await lane.CommitAsync(token);
    }
    private static async ValueTask<WorkspaceWrite> Start(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceRequestedCommand command, DateTimeOffset when, CancellationToken token)
    {
        for (int item = 0; item < RetryCount; item += 1)
        {
            WorkspaceItem? current = await Read(link, lane, command.Identity.ConversationKey, userId, token);
            IReadOnlyList<AccountData> list = await Accounts(link, lane, userId, token);
            WorkspaceData body = Home(list, string.Empty);
            string note = Json(body);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, HomeState, note, command.Payload, command.Payload, when);
            WorkspaceItem? next = current is null ? await Add(link, lane, frame, token) : await Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                return new WorkspaceWrite(next, current is null);
            }
        }
        throw new InvalidOperationException($"Workspace save exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }
    private static async ValueTask<WorkspaceWrite> Input(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceInputRequestedCommand command, DateTimeOffset when, CancellationToken token)
    {
        for (int item = 0; item < RetryCount; item += 1)
        {
            string mark = $"workspace_input_{item}";
            await Save(link, lane, mark, token);
            WorkspaceItem? current = await Read(link, lane, command.Identity.ConversationKey, userId, token);
            IReadOnlyList<AccountData> list = await Accounts(link, lane, userId, token);
            WorkspaceData body = current is null ? Home(list, string.Empty) : Sync(Data(current.Snapshot.Data), list);
            string state = current?.Snapshot.State ?? HomeState;
            WorkspaceMove move = await Flow(link, lane, userId, Move(state, body, command), when, token);
            string note = Json(move.Body);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, move.Code, note, string.Empty, command.Value, when);
            WorkspaceItem? next = current is null ? await Add(link, lane, frame, token) : await Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                await Release(link, lane, mark, token);
                return new WorkspaceWrite(next, current is null);
            }
            await Revert(link, lane, mark, token);
        }
        throw new InvalidOperationException($"Workspace input exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }
    private static async ValueTask<WorkspaceMove> Flow(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        move = await Pick(link, lane, userId, move, when, token);
        move = await Store(link, lane, userId, move, when, token);
        move = await Fill(link, lane, userId, move, token);
        move = await Track(link, lane, userId, move, when, token);
        move = await Correct(link, lane, userId, move, when, token);
        return await Finish(link, lane, userId, move, token);
    }
    private static WorkspaceMove Move(string state, WorkspaceData body, WorkspaceInputRequestedCommand command)
    {
        string kind = command.Kind.Trim();
        return kind switch
        {
            "action" => Act(state, body, command.Value),
            "text" => Text(state, body, command.Value),
            _ => new WorkspaceMove(state, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("Input kind is not supported", body.Status.Notice) }), null, string.Empty, null)
        };
    }
    private static WorkspaceMove Act(string state, WorkspaceData body, string value)
    {
        string code = value.Trim();
        if (code == AccountCancel && AccountState(state))
        {
            return new WorkspaceMove(HomeState, Reset(body, "Account creation was cancelled"), null, string.Empty, null);
        }
        if (code == ExpenseCancel && ExpenseState(state))
        {
            return new WorkspaceMove(HomeState, Reset(body, "Expense creation was cancelled"), null, string.Empty, null);
        }
        if (code == IncomeCancel && IncomeState(state))
        {
            return new WorkspaceMove(HomeState, Reset(body, "Income creation was cancelled"), null, string.Empty, null);
        }
        if (code == RecentBack && RecentState(state))
        {
            return RecentReturn(body, state);
        }
        return state switch
        {
            HomeState => HomeAction(body, code),
            CurrencyState => AccountCurrency(body, code),
            ConfirmState => AccountConfirm(body, code),
            ExpenseAccountState => TransactionAccount(body, code, false),
            ExpenseCategoryState => CategoryAction(body, code, false),
            ExpenseConfirmState => TransactionConfirm(body, code, false),
            IncomeAccountState => TransactionAccount(body, code, true),
            IncomeCategoryState => CategoryAction(body, code, true),
            IncomeConfirmState => TransactionConfirm(body, code, true),
            RecentListState => RecentListAction(body, code),
            RecentDetailState => RecentDetailAction(body, code),
            RecentDeleteState => RecentDeleteAction(body, code),
            RecentCategoryState => RecentCategoryAction(body, code),
            RecentRecategorizeState => RecentRecategorizeAction(body, code),
            _ => new WorkspaceMove(state, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("This action is not available", string.Empty) }), null, string.Empty, null)
        };
    }
    private static WorkspaceMove Text(string state, WorkspaceData body, string value) => state switch
    {
        HomeState when body.Accounts.Count == 0 => new WorkspaceMove(HomeState, Home(body.Accounts, AddAccountPrompt), null, string.Empty, null),
        HomeState => new WorkspaceMove(HomeState, Home(body.Accounts, ChooseActionPrompt), null, string.Empty, null),
        NameState => Draft(body, value),
        CurrencyState => Currency(body, value),
        BalanceState => Amount(body, value),
        ConfirmState => new WorkspaceMove(ConfirmState, AccountBody(body, body.Financial, new StatusData("Use the buttons to confirm or cancel", string.Empty)), null, string.Empty, null),
        ExpenseAccountState => new WorkspaceMove(ExpenseAccountState, Model(body, new WorkspaceFlowPatch { Financial = new FinancialData() }, new WorkspaceViewPatch { Status = new StatusData("Use the buttons to choose one account or cancel", string.Empty) }), null, string.Empty, null),
        ExpenseAmountState => TransactionAmount(body, value, false),
        ExpenseCategoryState => CategoryText(body, value, false),
        ExpenseConfirmState => new WorkspaceMove(ExpenseConfirmState, TransactionBody(body, AccountPick(body, false), CategoryValue(body, false), TransactionTotal(body, false), false, new ChoicesData(), new StatusData("Use the buttons to confirm or cancel", string.Empty)), null, string.Empty, null),
        IncomeAccountState => new WorkspaceMove(IncomeAccountState, Model(body, new WorkspaceFlowPatch { Financial = new FinancialData() }, new WorkspaceViewPatch { Status = new StatusData("Use the buttons to choose one account or cancel", string.Empty) }), null, string.Empty, null),
        IncomeAmountState => TransactionAmount(body, value, true),
        IncomeCategoryState => CategoryText(body, value, true),
        IncomeConfirmState => new WorkspaceMove(IncomeConfirmState, TransactionBody(body, AccountPick(body, true), CategoryValue(body, true), TransactionTotal(body, true), true, new ChoicesData(), new StatusData("Use the buttons to confirm or cancel", string.Empty)), null, string.Empty, null),
        RecentCategoryState => RecentCategoryText(body, value),
        RecentListState => new WorkspaceMove(RecentListState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("Use the buttons to choose one transaction or go back", string.Empty) }), null, string.Empty, null),
        RecentDetailState => new WorkspaceMove(RecentDetailState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("Use the buttons to continue", string.Empty) }), null, string.Empty, null),
        RecentDeleteState => new WorkspaceMove(RecentDeleteState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData(ConfirmGoBackPrompt, string.Empty) }), null, string.Empty, null),
        RecentRecategorizeState => new WorkspaceMove(RecentRecategorizeState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData(ConfirmGoBackPrompt, string.Empty) }), null, string.Empty, null),
        _ => new WorkspaceMove(HomeState, Home(body.Accounts, body.Accounts.Count == 0 ? AddAccountPrompt : ChooseActionPrompt), null, string.Empty, null)
    };
    private static WorkspaceMove HomeAction(WorkspaceData body, string code)
    {
        if (code == AddAccount)
        {
            return new WorkspaceMove(NameState, AccountBody(body, new FinancialData(string.Empty, string.Empty, null)), null, string.Empty, null);
        }
        if (code == AddExpense)
        {
            return TransactionStart(body, false);
        }
        if (code == AddIncome)
        {
            return TransactionStart(body, true);
        }
        if (code == ShowRecent)
        {
            return new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()), null, string.Empty, null);
        }
        return new WorkspaceMove(HomeState, Home(body.Accounts, body.Accounts.Count == 0 ? AddAccountPrompt : ChooseActionPrompt), null, string.Empty, null);
    }
    private static WorkspaceMove RecentListAction(WorkspaceData body, string code)
    {
        if (code == RecentPrevious)
        {
            int page = body.Recent.HasPrevious ? body.Recent.Page - 1 : body.Recent.Page;
            return new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(page < 0 ? 0 : page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()), null, string.Empty, null);
        }
        if (code == RecentNext)
        {
            int page = body.Recent.HasNext ? body.Recent.Page + 1 : body.Recent.Page;
            return new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()), null, string.Empty, null);
        }
        int slot = Slot(code, RecentItemSlot);
        RecentItemData? item = RecentItem(body.Recent.Items, slot);
        return item is null
            ? new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(RecentDetailState, RecentBody(body, new RecentData(body.Recent.Page, body.Recent.HasPrevious, body.Recent.HasNext, body.Recent.Items, new RecentItemData(item.Slot, new RecentEntryData(item.Id, item.Kind, item.Account, item.Category, item.Amount, item.Currency, item.OccurredUtc)))), null, string.Empty, null);
    }
    private static WorkspaceMove RecentDetailAction(WorkspaceData body, string code) => code switch
    {
        RecentDelete => RecentSelected(body, RecentDeleteState, TransactionMissingNotice),
        RecentRecategorize => RecentSelected(body, RecentCategoryState, TransactionMissingNotice),
        _ => new WorkspaceMove(RecentDetailState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("Use the buttons to continue", string.Empty) }), null, string.Empty, null)
    };
    private static WorkspaceMove RecentDeleteAction(WorkspaceData body, string code)
    {
        if (code != RecentDeleteApply)
        {
            return new WorkspaceMove(RecentDeleteState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData(ConfirmGoBackPrompt, string.Empty) }), null, string.Empty, null);
        }
        RecentItemData item = body.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(RecentDeleteState, body, null, string.Empty, null, new CorrectionNote(item.Id, item.Kind, DeleteMode, string.Empty));
    }
    private static WorkspaceMove RecentCategoryAction(WorkspaceData body, string code)
    {
        int slot = Slot(code, RecentCategorySlot);
        OptionData? item = Option(body.Choices.Categories, slot);
        if (item is null)
        {
            return new WorkspaceMove(RecentCategoryState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("Choose one category or send a new name", string.Empty) }), null, string.Empty, null);
        }
        RecentItemData selected = body.Recent.Selected;
        WorkspaceData state = RecentBody(body, new RecentData(body.Recent.Page, body.Recent.HasPrevious, body.Recent.HasNext, body.Recent.Items, new RecentItemData(selected.Slot, new RecentEntryData(selected.Id, selected.Kind, selected.Account, new PickData(item.Id, item.Name, item.Note), selected.Amount, selected.Currency, selected.OccurredUtc))));
        return new WorkspaceMove(RecentRecategorizeState, state, null, string.Empty, null);
    }
    private static WorkspaceMove RecentCategoryText(WorkspaceData body, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WorkspaceMove(RecentCategoryState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData("Category name is required", string.Empty) }), null, string.Empty, null);
        }
        RecentItemData item = body.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(RecentCategoryState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData() }), null, text, null, new CorrectionNote(item.Id, item.Kind, RecategorizeMode, string.Empty));
    }
    private static WorkspaceMove RecentRecategorizeAction(WorkspaceData body, string code)
    {
        if (code != RecentRecategorizeApply)
        {
            return new WorkspaceMove(RecentRecategorizeState, Model(body, view: new WorkspaceViewPatch { Status = new StatusData(ConfirmGoBackPrompt, string.Empty) }), null, string.Empty, null);
        }
        RecentItemData item = body.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Category.Id)
            ? new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(RecentRecategorizeState, body, null, string.Empty, null, new CorrectionNote(item.Id, item.Kind, RecategorizeMode, item.Category.Id));
    }
    private static WorkspaceMove RecentSelected(WorkspaceData body, string state, string notice)
    {
        RecentItemData item = body.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, notice)), null, string.Empty, null)
            : new WorkspaceMove(state, body, null, string.Empty, null);
    }
    private static WorkspaceMove RecentReturn(WorkspaceData body, string state) => state switch
    {
        RecentListState => new WorkspaceMove(HomeState, Home(body.Accounts, ChooseActionPrompt), null, string.Empty, null),
        RecentDetailState => new WorkspaceMove(RecentListState, RecentBody(body, new RecentData(body.Recent.Page, body.Recent.HasPrevious, body.Recent.HasNext, body.Recent.Items, new RecentItemData())), null, string.Empty, null),
        RecentDeleteState => new WorkspaceMove(RecentDetailState, body, null, string.Empty, null),
        RecentCategoryState => new WorkspaceMove(RecentDetailState, Model(body, view: new WorkspaceViewPatch { Choices = new ChoicesData(), Status = new StatusData() }), null, string.Empty, null),
        RecentRecategorizeState => new WorkspaceMove(RecentDetailState, body, null, string.Empty, null),
        _ => new WorkspaceMove(HomeState, Home(body.Accounts, body.Accounts.Count == 0 ? AddAccountPrompt : ChooseActionPrompt), null, string.Empty, null)
    };
    private static WorkspaceMove AccountCurrency(WorkspaceData body, string code) => code switch
    {
        Rub => Currency(body, "RUB"),
        Usd => Currency(body, "USD"),
        Eur => Currency(body, "EUR"),
        Other => new WorkspaceMove(CurrencyState, AccountBody(body, new FinancialData(body.Financial.Name, string.Empty, body.Financial.Amount), custom: true), null, string.Empty, null),
        _ => new WorkspaceMove(CurrencyState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Choose one currency option or send a 3 letter code", string.Empty), body.Custom), null, string.Empty, null)
    };
    private static WorkspaceMove AccountConfirm(WorkspaceData body, string code) => code == CreateAccountCode ? AccountCreate(body) : new WorkspaceMove(ConfirmState, AccountBody(body, body.Financial, new StatusData("Confirm the account or cancel", string.Empty)), null, string.Empty, null);
    private static WorkspaceMove TransactionStart(WorkspaceData body, bool income)
    {
        if (body.Accounts.Count == 0)
        {
            return new WorkspaceMove(HomeState, Home(body.Accounts, income ? "Add an account to record an income" : "Add an account to record an expense"), null, string.Empty, null);
        }
        if (body.Accounts.Count == 1)
        {
            AccountData item = body.Accounts[0];
            return new WorkspaceMove(TransactionAmountCode(income), TransactionBody(body, new PickData(item.Id, item.Name, item.Currency), new PickData(), null, income), null, string.Empty, null);
        }
        return new WorkspaceMove(TransactionAccountCode(income), TransactionBody(body, new PickData(), new PickData(), null, income, new ChoicesData(AccountChoices(body.Accounts), [])), null, string.Empty, null);
    }
    private static WorkspaceMove TransactionAccount(WorkspaceData body, string code, bool income)
    {
        int slot = Slot(code, TransactionAccountSlot(income));
        OptionData? item = Option(body.Choices.Accounts, slot);
        return item is null
            ? new WorkspaceMove(TransactionAccountCode(income), Model(body, new WorkspaceFlowPatch { Financial = new FinancialData() }, new WorkspaceViewPatch { Status = new StatusData("Choose one account", string.Empty) }), null, string.Empty, null)
            : new WorkspaceMove(TransactionAmountCode(income), TransactionBody(body, new PickData(item.Id, item.Name, item.Note), new PickData(), null, income), null, string.Empty, null);
    }
    private static WorkspaceMove CategoryAction(WorkspaceData body, string code, bool income)
    {
        int slot = Slot(code, TransactionCategorySlot(income));
        OptionData? item = Option(body.Choices.Categories, slot);
        return item is null
            ? new WorkspaceMove(TransactionCategoryCode(income), Model(body, new WorkspaceFlowPatch { Financial = new FinancialData() }, new WorkspaceViewPatch { Status = new StatusData("Choose one category or send a new name", string.Empty) }), null, string.Empty, null)
            : new WorkspaceMove(TransactionConfirmCode(income), TransactionBody(body, AccountPick(body, income), new PickData(item.Id, item.Name, item.Note), TransactionTotal(body, income), income), null, string.Empty, null);
    }
    private static WorkspaceMove TransactionConfirm(WorkspaceData body, string code, bool income)
    {
        if (code == TransactionCreateCode(income))
        {
            return TransactionCreate(body, income);
        }
        string text = income ? "Confirm the income or cancel" : "Confirm the expense or cancel";
        return new WorkspaceMove(TransactionConfirmCode(income), TransactionBody(body, AccountPick(body, income), CategoryValue(body, income), TransactionTotal(body, income), income, new ChoicesData(), new StatusData(text, string.Empty)), null, string.Empty, null);
    }
    private static WorkspaceMove Draft(WorkspaceData body, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WorkspaceMove(NameState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Account name is required", string.Empty)), null, string.Empty, null);
        }
        if (body.Financial.Amount.HasValue && !string.IsNullOrWhiteSpace(body.Financial.Currency))
        {
            return new WorkspaceMove(ConfirmState, AccountBody(body, new FinancialData(text, body.Financial.Currency, body.Financial.Amount)), null, string.Empty, null);
        }
        if (!string.IsNullOrWhiteSpace(body.Financial.Currency))
        {
            return new WorkspaceMove(BalanceState, AccountBody(body, new FinancialData(text, body.Financial.Currency, body.Financial.Amount)), null, string.Empty, null);
        }
        return new WorkspaceMove(CurrencyState, AccountBody(body, new FinancialData(text, string.Empty, body.Financial.Amount)), null, string.Empty, null);
    }
    private static WorkspaceMove Currency(WorkspaceData body, string value)
    {
        string text = value.Trim().ToUpperInvariant();
        bool valid = text.Length == 3 && text.All(char.IsLetter);
        if (!valid)
        {
            return new WorkspaceMove(CurrencyState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Currency code must contain 3 letters", string.Empty), true), null, string.Empty, null);
        }
        return body.Financial.Amount.HasValue
            ? new WorkspaceMove(ConfirmState, AccountBody(body, new FinancialData(body.Financial.Name, text, body.Financial.Amount)), null, string.Empty, null)
            : new WorkspaceMove(BalanceState, AccountBody(body, new FinancialData(body.Financial.Name, text, body.Financial.Amount)), null, string.Empty, null);
    }
    private static WorkspaceMove Amount(WorkspaceData body, string value)
    {
        bool ok = TryAmount(value, out decimal amount);
        return ok
            ? new WorkspaceMove(ConfirmState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, amount)), null, string.Empty, null)
            : new WorkspaceMove(BalanceState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Balance must be a number", string.Empty)), null, string.Empty, null);
    }
    private static WorkspaceMove TransactionAmount(WorkspaceData body, string value, bool income)
    {
        bool ok = TryAmount(value, out decimal amount);
        if (!ok)
        {
            return new WorkspaceMove(TransactionAmountCode(income), TransactionBody(body, AccountPick(body, income), new PickData(), TransactionTotal(body, income), income, new ChoicesData(), new StatusData("Enter a valid numeric amount", string.Empty)), null, string.Empty, null);
        }
        if (Scale(amount) > 4)
        {
            return new WorkspaceMove(TransactionAmountCode(income), TransactionBody(body, AccountPick(body, income), new PickData(), TransactionTotal(body, income), income, new ChoicesData(), new StatusData("Enter up to 4 decimal places", string.Empty)), null, string.Empty, null);
        }
        if (amount <= 0m)
        {
            return new WorkspaceMove(TransactionAmountCode(income), TransactionBody(body, AccountPick(body, income), new PickData(), TransactionTotal(body, income), income, new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty)), null, string.Empty, null);
        }
        return new WorkspaceMove(TransactionCategoryCode(income), TransactionBody(body, AccountPick(body, income), new PickData(), amount, income), null, string.Empty, null);
    }
    private static WorkspaceMove CategoryText(WorkspaceData body, string value, bool income)
    {
        string text = value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? new WorkspaceMove(TransactionCategoryCode(income), Model(body, new WorkspaceFlowPatch { Financial = new FinancialData() }, new WorkspaceViewPatch { Status = new StatusData("Category name is required", string.Empty) }), null, string.Empty, null)
            : new WorkspaceMove(TransactionCategoryCode(income), Model(body, new WorkspaceFlowPatch { Financial = new FinancialData() }, new WorkspaceViewPatch { Status = new StatusData() }), null, text, null);
    }
    private static WorkspaceMove AccountCreate(WorkspaceData body)
    {
        if (string.IsNullOrWhiteSpace(body.Financial.Name))
        {
            return new WorkspaceMove(NameState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Account name is required", string.Empty)), null, string.Empty, null);
        }
        if (string.IsNullOrWhiteSpace(body.Financial.Currency))
        {
            return new WorkspaceMove(CurrencyState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Currency code must contain 3 letters", string.Empty), true), null, string.Empty, null);
        }
        if (!body.Financial.Amount.HasValue)
        {
            return new WorkspaceMove(BalanceState, AccountBody(body, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new StatusData("Balance must be a number", string.Empty)), null, string.Empty, null);
        }
        return new WorkspaceMove(HomeState, Reset(body, "Account was created"), new AccountDraft(body.Financial.Name, body.Financial.Currency, body.Financial.Amount.Value), string.Empty, null);
    }
    private static WorkspaceMove TransactionCreate(WorkspaceData body, bool income)
    {
        string accountId = Resolve(body, income);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return TransactionStart(body, income);
        }
        decimal? total = TransactionTotal(body, income);
        if (!total.HasValue || total.Value <= 0m)
        {
            return new WorkspaceMove(TransactionAmountCode(income), TransactionBody(body, AccountPick(body, income), new PickData(), total, income, new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty)), null, string.Empty, null);
        }
        PickData category = CategoryValue(body, income);
        if (string.IsNullOrWhiteSpace(category.Id))
        {
            return new WorkspaceMove(TransactionCategoryCode(income), TransactionBody(body, AccountPick(body, income), new PickData(), total, income, new ChoicesData(), new StatusData("Choose one category or send a new name", string.Empty)), null, string.Empty, null);
        }
        PickData account = AccountPick(body, income);
        WorkspaceData item = TransactionBody(body, new PickData(accountId, account.Name, account.Note), category, total, income);
        return new WorkspaceMove(TransactionConfirmCode(income), item, null, string.Empty, new TransactionNote(accountId, category.Id, total.Value, Kind(income)));
    }
    private static WorkspaceData Reset(WorkspaceData body, string notice) => new(body.Accounts, new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), new StatusData(string.Empty, notice), false));
    private static WorkspaceData Home(IReadOnlyList<AccountData> list, string notice, string error = "") => new(list, new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), new StatusData(error, notice), false));
    private static WorkspaceData Sync(WorkspaceData body, IReadOnlyList<AccountData> list) => new(list, new WorkspaceStateData(body.Financial, body.Expense, body.Income, body.Recent, body.Choices, body.Status, body.Custom));
    private static WorkspaceData Data(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkspaceData();
        }
        WorkspaceData? item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        return new WorkspaceData(item?.Accounts ?? [], new WorkspaceStateData(item?.Financial ?? new FinancialData(), item?.Expense ?? new ExpenseData(), item?.Income ?? new IncomeData(), item?.Recent ?? new RecentData(), item?.Choices ?? new ChoicesData(), item?.Status ?? new StatusData(), item?.Custom ?? false));
    }
    private static string Json(WorkspaceData item) => JsonSerializer.Serialize(item, json);
    private static WorkspaceActionContext Context(WorkspaceData body) => new(body.Accounts.Count, body.Choices.Accounts.Count, body.Choices.Categories.Count, body.Recent.Items.Count, new RecentPaging(body.Recent.HasPrevious, body.Recent.HasNext), body.Custom);
    private static DateTimeOffset Utc(DateTimeOffset value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, default);
        return value.Offset == TimeSpan.Zero ? value : throw new ArgumentException("Workspace occurrence time must be UTC", name);
    }
    private static int Slot(string value, string prefix)
    {
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return 0;
        }
        return int.TryParse(value[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int slot) && slot > 0 ? slot : 0;
    }
    private static bool TryAmount(string value, out decimal amount)
    {
        string text = value.Trim();
        if (Decimal(text, ',', out amount) && Candidate(text, ','))
        {
            return true;
        }
        if (Decimal(text, '.', out amount) && Candidate(text, '.'))
        {
            return true;
        }
        bool current = decimal.TryParse(text, AmountStyles, CultureInfo.CurrentCulture, out decimal local);
        bool invariant = decimal.TryParse(text, AmountStyles, CultureInfo.InvariantCulture, out decimal global);
        if (string.IsNullOrWhiteSpace(text) || text.Contains('.') && text.Contains(',') || Delimiter(text) && current && invariant && local != global)
        {
            amount = 0m;
            return false;
        }
        if (current)
        {
            amount = local;
            return true;
        }
        amount = global;
        return invariant;
    }
    private static int Scale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;
    private static bool Candidate(string value, char sign)
    {
        int slot = value.LastIndexOf(sign);
        return slot > 0 && slot < value.Length - 1 && Numeric(value[..slot], true) && Numeric(value[(slot + 1)..], false) && value[(slot + 1)..].Length <= 4 && !Grouped(value, sign);
    }
    private static bool Grouped(string value, char sign)
    {
        string text = value[0] is '+' or '-' ? value[1..] : value;
        string[] list = text.Split(sign);
        return list.Length > 1 && list[0].Length is > 0 and <= 3 && list[^1].Length == 3 && list.All(Numeric) && list.Skip(1).All(item => item.Length == 3);
    }
    private static bool Numeric(string value) => value.Length > 0 && value.All(char.IsDigit);
    private static bool Numeric(string value, bool signed) => signed && value[0] is '+' or '-' ? Numeric(value[1..]) : Numeric(value);
    private static bool Decimal(string value, char sign, out decimal amount)
    {
        var item = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        item.NumberDecimalSeparator = sign.ToString();
        item.NumberGroupSeparator = sign == ',' ? "." : ",";
        return decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, item, out amount);
    }
    private static bool Delimiter(string value) => value.Contains('.') || value.Contains(',') || value.Contains(' ') || value.Contains('\u00A0');
    private static bool AccountState(string state) => state is NameState or CurrencyState or BalanceState or ConfirmState;
    private static bool ExpenseState(string state) => state is ExpenseAccountState or ExpenseAmountState or ExpenseCategoryState or ExpenseConfirmState;
    private static bool IncomeState(string state) => state is IncomeAccountState or IncomeAmountState or IncomeCategoryState or IncomeConfirmState;
    private static bool RecentState(string state) => state is RecentListState or RecentDetailState or RecentDeleteState or RecentCategoryState or RecentRecategorizeState;
    private static bool TransactionCategoryState(string state) => state is ExpenseCategoryState or IncomeCategoryState or RecentCategoryState;
    private static OptionData? Option(IReadOnlyList<OptionData> list, int slot) => list.SingleOrDefault(item => item.Slot == slot);
    private static RecentItemData? RecentItem(IReadOnlyList<RecentItemData> list, int slot) => list.SingleOrDefault(item => item.Slot == slot);
    private static IReadOnlyList<OptionData> AccountChoices(IReadOnlyList<AccountData> list) => [.. list.Select((item, index) => new OptionData(index + 1, item.Id, item.Name, item.Currency))];
    private static string Resolve(WorkspaceData body, bool income)
    {
        PickData account = AccountPick(body, income);
        if (!string.IsNullOrWhiteSpace(account.Id))
        {
            return account.Id;
        }
        AccountData? item = body.Accounts.FirstOrDefault(candidate => candidate.Name == account.Name && candidate.Currency == account.Note);
        return item?.Id ?? string.Empty;
    }
    private static async ValueTask<WorkspaceMove> Pick(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token) => string.IsNullOrWhiteSpace(move.CategoryEntry) ? move : await CategoryPick(link, lane, userId, move, when, token);
    private static async ValueTask<WorkspaceMove> Store(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.AccountValue is null)
        {
            return move;
        }
        bool fresh = await Account(link, lane, userId, move.AccountValue, when, token);
        return fresh ? move : new WorkspaceMove(NameState, AccountBody(move.Body, new FinancialData(move.AccountValue.Title, move.AccountValue.Unit, move.AccountValue.Total), new StatusData("Account name already exists", string.Empty)), null, string.Empty, null);
    }
    private static async ValueTask<WorkspaceMove> Fill(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, CancellationToken token)
    {
        if (move.Code == RecentListState && move.Body.Recent.Items.Count == 0)
        {
            RecentData page = await Recent(link, lane, userId, move.Body.Recent.Page, token);
            return new WorkspaceMove(RecentListState, RecentBody(move.Body, page, new ChoicesData(), move.Body.Status), null, string.Empty, null);
        }
        if (!TransactionCategoryState(move.Code) || move.Body.Choices.Categories.Count > 0)
        {
            return move;
        }
        string kind = move.Code == RecentCategoryState ? move.Body.Recent.Selected.Kind : Kind(move.Code);
        IReadOnlyList<OptionData> list = await Categories(link, lane, userId, kind, token);
        return new WorkspaceMove(move.Code, Model(move.Body, view: new WorkspaceViewPatch { Choices = new ChoicesData([], list) }), null, string.Empty, null);
    }
    private static async ValueTask<WorkspaceMove> Track(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.RecordValue is null)
        {
            return move;
        }
        await Transaction(link, lane, userId, move.RecordValue, when, token);
        return new WorkspaceMove(HomeState, Home(await Accounts(link, lane, userId, token), move.RecordValue.TransactionKind == IncomeKind ? "Income was recorded" : "Expense was recorded"), null, string.Empty, null);
    }
    private static async ValueTask<WorkspaceMove> Correct(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.CorrectValue is null)
        {
            return move;
        }
        if (move.CorrectValue.Mode == DeleteMode)
        {
            bool ok = await Delete(link, lane, userId, move.CorrectValue.TransactionId, move.CorrectValue.TransactionKind, when, token);
            RecentData page = await Current(link, lane, userId, move.Body.Recent.Page, token);
            return new WorkspaceMove(RecentListState, RecentBody(move.Body, page, new ChoicesData(), new StatusData(string.Empty, ok ? "Transaction was deleted" : TransactionMissingNotice)), null, string.Empty, null);
        }
        bool fresh = await Recategorize(link, lane, userId, move.CorrectValue, when, token);
        RecentData current = await Recent(link, lane, userId, move.Body.Recent.Page, token);
        return new WorkspaceMove(RecentListState, RecentBody(move.Body, current, new ChoicesData(), new StatusData(string.Empty, fresh ? "Category was updated" : TransactionMissingNotice)), null, string.Empty, null);
    }
    private static async ValueTask<WorkspaceMove> Finish(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, CancellationToken token)
    {
        if (move.Code == HomeState)
        {
            return new WorkspaceMove(HomeState, Home(await Accounts(link, lane, userId, token), move.Body.Status.Notice, move.Body.Status.Error), null, string.Empty, null);
        }
        if (move.Code == RecentDetailState && !string.IsNullOrWhiteSpace(move.Body.Recent.Selected.Id))
        {
            RecentItemData? item = await Recent(link, lane, userId, move.Body.Recent.Selected.Id, token);
            if (item is null)
            {
                RecentData page = await Current(link, lane, userId, move.Body.Recent.Page, token);
                return new WorkspaceMove(RecentListState, RecentBody(move.Body, page, new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null);
            }
            return new WorkspaceMove(RecentDetailState, RecentBody(move.Body, new RecentData(move.Body.Recent.Page, move.Body.Recent.HasPrevious, move.Body.Recent.HasNext, move.Body.Recent.Items, item), move.Body.Choices, move.Body.Status), null, string.Empty, null);
        }
        return move;
    }
    private static async ValueTask<WorkspaceMove> CategoryPick(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        if (move.Code == RecentCategoryState && move.CorrectValue is not null)
        {
            if (string.IsNullOrWhiteSpace(move.Body.Recent.Selected.Id))
            {
                RecentData page = await Current(link, lane, userId, move.Body.Recent.Page, token);
                return new WorkspaceMove(RecentListState, RecentBody(move.Body, page, new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null);
            }
            RecentItemData? current = await Recent(link, lane, userId, move.Body.Recent.Selected.Id, token);
            if (current is null)
            {
                RecentData page = await Current(link, lane, userId, move.Body.Recent.Page, token);
                return new WorkspaceMove(RecentListState, RecentBody(move.Body, page, new ChoicesData(), new StatusData(string.Empty, TransactionMissingNotice)), null, string.Empty, null);
            }
            PickData pick = await Category(link, lane, userId, move.CategoryEntry, move.CorrectValue.TransactionKind, when, token);
            WorkspaceData recent = RecentBody(move.Body, new RecentData(move.Body.Recent.Page, move.Body.Recent.HasPrevious, move.Body.Recent.HasNext, move.Body.Recent.Items, new RecentItemData(current.Slot, new RecentEntryData(current.Id, current.Kind, current.Account, pick, current.Amount, current.Currency, current.OccurredUtc))), move.Body.Choices, move.Body.Status);
            return new WorkspaceMove(RecentRecategorizeState, recent, null, string.Empty, null);
        }
        bool income = Kind(move.Code) == IncomeKind;
        PickData item = await Category(link, lane, userId, move.CategoryEntry, Kind(income), when, token);
        WorkspaceData body = TransactionBody(move.Body, AccountPick(move.Body, income), item, TransactionTotal(move.Body, income), income);
        return new WorkspaceMove(TransactionConfirmCode(income), body, null, string.Empty, null);
    }
    private static WorkspaceData Model(WorkspaceData body, WorkspaceFlowPatch? flow = null, WorkspaceViewPatch? view = null) => new(body.Accounts, new WorkspaceStateData(flow?.Financial ?? body.Financial, body.Expense, body.Income, body.Recent, view?.Choices ?? body.Choices, view?.Status ?? body.Status, body.Custom));
    private sealed record WorkspaceFlowPatch
    {
        public FinancialData? Financial { get; init; }
    }
    private sealed record WorkspaceViewPatch
    {
        public ChoicesData? Choices { get; init; }
        public StatusData? Status { get; init; }
    }
    private static WorkspaceData AccountBody(WorkspaceData body, FinancialData financial, StatusData? status = null, bool custom = false) => new(body.Accounts, new WorkspaceStateData(financial, new ExpenseData(), new IncomeData(), new RecentData(), new ChoicesData(), status ?? new StatusData(), custom));
    private static WorkspaceData TransactionBody(WorkspaceData body, PickData account, PickData category, decimal? amount, bool income, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData(new FinancialData(), income ? new ExpenseData() : new ExpenseData(account, category, amount), income ? new IncomeData(account, category, amount) : new IncomeData(), new RecentData(), choices ?? new ChoicesData(), status ?? new StatusData(), false));
    private static WorkspaceData RecentBody(WorkspaceData body, RecentData recent, ChoicesData? choices = null, StatusData? status = null) => new(body.Accounts, new WorkspaceStateData(new FinancialData(), new ExpenseData(), new IncomeData(), recent, choices ?? new ChoicesData(), status ?? new StatusData(), false));
    private static PickData AccountPick(WorkspaceData body, bool income) => income ? body.Income.Account : body.Expense.Account;
    private static PickData CategoryValue(WorkspaceData body, bool income) => income ? body.Income.Category : body.Expense.Category;
    private static decimal? TransactionTotal(WorkspaceData body, bool income) => income ? body.Income.Amount : body.Expense.Amount;
    private static string Kind(string state) => state.StartsWith("transaction.income.", StringComparison.Ordinal) ? IncomeKind : ExpenseKind;
    private static string Kind(bool income) => income ? IncomeKind : ExpenseKind;
    private static string Supported(string kind) => kind switch
    {
        IncomeKind => IncomeKind,
        ExpenseKind => ExpenseKind,
        _ => throw new InvalidOperationException($"Transaction kind '{kind}' is not supported")
    };
    private static string Change(string kind) => Supported(kind) switch
    {
        IncomeKind => "+",
        ExpenseKind => "-",
        _ => throw new InvalidOperationException($"Transaction kind '{kind}' is not supported")
    };
    private static string Reverse(string kind) => Supported(kind) switch
    {
        IncomeKind => "-",
        ExpenseKind => "+",
        _ => throw new InvalidOperationException($"Transaction kind '{kind}' is not supported")
    };
    private static string TransactionAccountCode(bool income) => income ? IncomeAccountState : ExpenseAccountState;
    private static string TransactionAmountCode(bool income) => income ? IncomeAmountState : ExpenseAmountState;
    private static string TransactionCategoryCode(bool income) => income ? IncomeCategoryState : ExpenseCategoryState;
    private static string TransactionConfirmCode(bool income) => income ? IncomeConfirmState : ExpenseConfirmState;
    private static string TransactionCreateCode(bool income) => income ? CreateIncomeCode : CreateExpenseCode;
    private static string TransactionAccountSlot(bool income) => income ? IncomeAccountSlot : ExpenseAccountSlot;
    private static string TransactionCategorySlot(bool income) => income ? IncomeCategorySlot : ExpenseCategorySlot;
    private static async ValueTask Save(NpgsqlConnection link, NpgsqlTransaction lane, string mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new($"savepoint {mark}", link, lane);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    private static async ValueTask Revert(NpgsqlConnection link, NpgsqlTransaction lane, string mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new($"rollback to savepoint {mark}", link, lane);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    private static async ValueTask Release(NpgsqlConnection link, NpgsqlTransaction lane, string mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new($"release savepoint {mark}", link, lane);
        _ = await note.ExecuteNonQueryAsync(token);
    }
    private static async ValueTask<bool> Inbox<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, string payload, CancellationToken token) where TMessage : class
    {
        await using NpgsqlCommand note = new("insert into finance.inbox_message(message_id, contract, source, correlation_id, causation_id, idempotency_key, payload, received_utc, processed_utc, attempt) values (@message_id, @contract, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @received_utc, @processed_utc, @attempt) on conflict do nothing", link, lane);
        note.Parameters.AddWithValue("message_id", message.MessageId);
        note.Parameters.AddWithValue("contract", message.Contract);
        note.Parameters.AddWithValue("source", message.Source);
        note.Parameters.AddWithValue("correlation_id", message.Context.CorrelationId);
        note.Parameters.AddWithValue("causation_id", message.Context.CausationId);
        note.Parameters.AddWithValue("idempotency_key", message.Context.IdempotencyKey);
        note.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payload);
        note.Parameters.AddWithValue("received_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("processed_utc", DBNull.Value);
        note.Parameters.AddWithValue("attempt", 1);
        return await note.ExecuteNonQueryAsync(token) == 1;
    }
    private static async ValueTask<(Guid UserId, bool IsNewUser)> User(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceIdentity identity, WorkspaceProfile profile, DateTimeOffset when, CancellationToken token)
    {
        await using NpgsqlCommand add = new("insert into finance.user_account(id, actor_key, name, locale, created_utc, updated_utc) values (@id, @actor_key, @name, @locale, @created_utc, @updated_utc) on conflict do nothing returning id", link, lane);
        add.Parameters.AddWithValue("id", Guid.CreateVersion7());
        add.Parameters.AddWithValue("actor_key", identity.ActorKey);
        add.Parameters.AddWithValue("name", profile.Name);
        add.Parameters.AddWithValue("locale", profile.Locale);
        add.Parameters.AddWithValue(CreatedUtc, when);
        add.Parameters.AddWithValue(UpdatedUtc, when);
        Guid? userId = await Id(add, token);
        if (userId.HasValue)
        {
            return (userId.Value, true);
        }
        await using NpgsqlCommand note = new("update finance.user_account set name = @name, locale = @locale, updated_utc = @updated_utc where actor_key = @actor_key returning id", link, lane);
        note.Parameters.AddWithValue("actor_key", identity.ActorKey);
        note.Parameters.AddWithValue("name", profile.Name);
        note.Parameters.AddWithValue("locale", profile.Locale);
        note.Parameters.AddWithValue(UpdatedUtc, when);
        userId = await Id(note, token);
        if (userId.HasValue)
        {
            return (userId.Value, false);
        }
        throw new InvalidOperationException("User upsert failed");
    }
    private static async ValueTask<WorkspaceItem?> Read(NpgsqlConnection link, NpgsqlTransaction lane, string key, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id, state_code, state_data, revision from finance.workspace where conversation_key = @conversation_key and user_id = @user_id for update", link, lane);
        note.Parameters.AddWithValue("conversation_key", key);
        note.Parameters.AddWithValue(UserId, userId);
        return await Item(note, false, token);
    }
    private static async ValueTask<WorkspaceItem?> Add(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceFrame frame, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.workspace(id, user_id, conversation_key, state_code, state_data, revision, entry_payload, last_payload, created_utc, opened_utc, updated_utc) values (@id, @user_id, @conversation_key, @state_code, @state_data, @revision, @entry_payload, @last_payload, @created_utc, @opened_utc, @updated_utc) on conflict (user_id, conversation_key) do nothing returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("id", Guid.CreateVersion7());
        note.Parameters.AddWithValue(UserId, frame.UserValue);
        note.Parameters.AddWithValue("conversation_key", frame.Room);
        note.Parameters.AddWithValue("state_code", frame.State);
        note.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, frame.Body);
        note.Parameters.AddWithValue("revision", 1L);
        note.Parameters.AddWithValue("entry_payload", frame.Entry);
        note.Parameters.AddWithValue("last_payload", frame.Last);
        note.Parameters.AddWithValue(CreatedUtc, frame.When);
        note.Parameters.AddWithValue("opened_utc", frame.When);
        note.Parameters.AddWithValue(UpdatedUtc, frame.When);
        return await Item(note, true, token);
    }
    private static async ValueTask<WorkspaceItem?> Write(NpgsqlConnection link, NpgsqlTransaction lane, WorkspaceMark mark, CancellationToken token)
    {
        await using NpgsqlCommand note = new("update finance.workspace set user_id = @user_id, state_code = @state_code, state_data = @state_data, last_payload = @last_payload, revision = revision + 1, opened_utc = @opened_utc, updated_utc = @updated_utc where id = @id and revision = @revision and user_id = @user_id returning id, state_code, state_data, revision", link, lane);
        note.Parameters.AddWithValue("id", mark.IdValue);
        note.Parameters.AddWithValue("revision", mark.Revision);
        note.Parameters.AddWithValue(UserId, mark.Frame.UserValue);
        note.Parameters.AddWithValue("state_code", mark.Frame.State);
        note.Parameters.AddWithValue("state_data", NpgsqlDbType.Jsonb, mark.Frame.Body);
        note.Parameters.AddWithValue("last_payload", mark.Frame.Last);
        note.Parameters.AddWithValue("opened_utc", mark.Frame.When);
        note.Parameters.AddWithValue(UpdatedUtc, mark.Frame.When);
        return await Item(note, false, token);
    }
    private static async ValueTask<bool> Account(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, AccountDraft draft, DateTimeOffset when, CancellationToken token)
    {
        await using NpgsqlCommand note = new("insert into finance.account(id, user_id, name, currency_code, opening_amount, current_amount, created_utc, updated_utc) values (@id, @user_id, @name, @currency_code, @opening_amount, @current_amount, @created_utc, @updated_utc) on conflict do nothing returning id", link, lane);
        note.Parameters.AddWithValue("id", Guid.CreateVersion7());
        note.Parameters.AddWithValue(UserId, userId);
        note.Parameters.AddWithValue("name", draft.Title);
        note.Parameters.AddWithValue("currency_code", draft.Unit);
        note.Parameters.AddWithValue("opening_amount", draft.Total);
        note.Parameters.AddWithValue("current_amount", draft.Total);
        note.Parameters.AddWithValue(CreatedUtc, when);
        note.Parameters.AddWithValue(UpdatedUtc, when);
        return (await Id(note, token)).HasValue;
    }
    private static async ValueTask<IReadOnlyList<AccountData>> Accounts(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CancellationToken token)
    {
        await using NpgsqlCommand note = new("select id::text, name, currency_code, current_amount from finance.account where user_id = @user_id order by created_utc, name", link, lane);
        note.Parameters.AddWithValue(UserId, userId);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        List<AccountData> list = [];
        while (await row.ReadAsync(token))
        {
            list.Add(new AccountData(row.GetString(0), row.GetString(1), row.GetString(2), row.GetDecimal(3)));
        }
        return list;
    }
    private static async ValueTask<IReadOnlyList<OptionData>> Categories(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string kind, CancellationToken token)
    {
        List<OptionData> list = [];
        await using NpgsqlCommand note = new("""
                                             with recent(category_id, occurred_utc) as
                                             (
                                                 select category_id, max(occurred_utc) as occurred_utc
                                                 from finance.transaction_entry
                                                 where user_id = @user_id and kind = @kind
                                                 group by category_id
                                             ),
                                             custom(id, name, code, area, order_id, occurred_utc) as
                                             (
                                                 select c.id::text, c.name, '' as code, 1 as area, 999 as order_id, recent.occurred_utc
                                                 from finance.category c
                                                 join recent on recent.category_id = c.id
                                                 where c.user_id = @user_id and c.kind = @kind and c.scope = 'user'
                                                 order by recent.occurred_utc desc, c.name
                                                 limit 6
                                             )
                                             select id, name, code
                                             from
                                             (
                                                 select id::text, name, coalesce(code, '') as code, 0 as area,
                                                        case
                                                            when @kind = 'expense' then case code when 'food' then 1 when 'transport' then 2 when 'home' then 3 when 'health' then 4 when 'shopping' then 5 when 'fun' then 6 when 'bills' then 7 when 'travel' then 8 else 999 end
                                                            else case code when 'salary' then 1 when 'bonus' then 2 when 'gift' then 3 when 'cashback' then 4 when 'sale' then 5 when 'interest' then 6 when 'refund' then 7 when 'other' then 8 else 999 end
                                                        end as order_id,
                                                        null::timestamptz as occurred_utc
                                                 from finance.category
                                                 where kind = @kind and scope = 'system'
                                                 union all
                                                 select id, name, code, area, order_id, occurred_utc
                                                 from custom
                                             ) item
                                             order by area, order_id, occurred_utc desc nulls last, name
                                             """, link, lane);
        note.Parameters.AddWithValue(UserId, userId);
        note.Parameters.AddWithValue("kind", kind);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        while (await row.ReadAsync(token))
        {
            list.Add(new OptionData(list.Count + 1, row.GetString(0), row.GetString(1), row.GetString(2)));
        }
        return list;
    }
    private static async ValueTask<RecentData> Recent(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, int page, CancellationToken token)
    {
        int index = Page(page, 0);
        int offset = index * RecentPageSize;
        List<RecentItemData> list = [];
        await using NpgsqlCommand note = new("""
                                             select item.id::text,
                                                    item.kind,
                                                    account.id::text,
                                                    account.name,
                                                    account.currency_code,
                                                    category.id::text,
                                                    category.name,
                                                    coalesce(category.code, ''),
                                                    item.amount,
                                                    item.occurred_utc
                                             from finance.transaction_entry item
                                             join finance.account account on account.id = item.account_id
                                             join finance.category category on category.id = item.category_id
                                             where item.user_id = @user_id
                                             order by item.occurred_utc desc, item.created_utc desc, item.id desc
                                             offset @offset
                                             limit @limit
                                             """, link, lane);
        note.Parameters.AddWithValue(UserId, userId);
        note.Parameters.AddWithValue("offset", offset);
        note.Parameters.AddWithValue("limit", RecentPageSize + 1);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        while (await row.ReadAsync(token))
        {
            list.Add(new RecentItemData(
                list.Count + 1,
                new RecentEntryData(
                    row.GetString(0),
                    row.GetString(1),
                    new PickData(row.GetString(2), row.GetString(3), row.GetString(4)),
                    new PickData(row.GetString(5), row.GetString(6), row.GetString(7)),
                    row.GetDecimal(8),
                    row.GetString(4),
                    await row.GetFieldValueAsync<DateTimeOffset>(9, token))));
        }
        bool hasNext = list.Count > RecentPageSize;
        RecentItemData[] items = hasNext ? [.. list.Take(RecentPageSize)] : [.. list];
        for (int item = 0; item < items.Length; item += 1)
        {
            items[item] = new RecentItemData(item + 1, new RecentEntryData(items[item].Id, items[item].Kind, items[item].Account, items[item].Category, items[item].Amount, items[item].Currency, items[item].OccurredUtc));
        }
        return new RecentData(index, index > 0, hasNext, items, new RecentItemData());
    }
    private static async ValueTask<RecentItemData?> Recent(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string transactionId, CancellationToken token)
    {
        Guid itemId = Parse(transactionId, nameof(transactionId));
        await using NpgsqlCommand note = new("""
                                             select item.id::text,
                                                    item.kind,
                                                    account.id::text,
                                                    account.name,
                                                    account.currency_code,
                                                    category.id::text,
                                                    category.name,
                                                    coalesce(category.code, ''),
                                                    item.amount,
                                                    item.occurred_utc
                                             from finance.transaction_entry item
                                             join finance.account account on account.id = item.account_id
                                             join finance.category category on category.id = item.category_id
                                             where item.user_id = @user_id and item.id = @id
                                             limit 1
                                             """, link, lane);
        note.Parameters.AddWithValue(UserId, userId);
        note.Parameters.AddWithValue("id", itemId);
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        if (!await row.ReadAsync(token))
        {
            return null;
        }
        return new RecentItemData(0, new RecentEntryData(row.GetString(0), row.GetString(1), new PickData(row.GetString(2), row.GetString(3), row.GetString(4)), new PickData(row.GetString(5), row.GetString(6), row.GetString(7)), row.GetDecimal(8), row.GetString(4), await row.GetFieldValueAsync<DateTimeOffset>(9, token)));
    }
    private static async ValueTask<RecentData> Current(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, int page, CancellationToken token)
    {
        RecentData item = await Recent(link, lane, userId, Page(page, 0), token);
        while (item.Items.Count == 0 && item.Page > 0)
        {
            item = await Recent(link, lane, userId, Page(item.Page, -1), token);
        }
        return item;
    }
    private static int Page(int page, int shift)
    {
        int item = page + shift;
        return item < 0 ? 0 : item;
    }
    private static async ValueTask<PickData> Category(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string value, string kind, DateTimeOffset when, CancellationToken token)
    {
        string text = value.Trim();
        await using (NpgsqlCommand find = new("select id::text, name, coalesce(code, '') from finance.category where kind = @kind and lower(name) = lower(@name) and (scope = 'system' or user_id = @user_id) order by case scope when 'system' then 0 else 1 end limit 1", link, lane))
        {
            find.Parameters.AddWithValue("kind", kind);
            find.Parameters.AddWithValue("name", text);
            find.Parameters.AddWithValue(UserId, userId);
            await using NpgsqlDataReader row = await find.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), row.GetString(2));
            }
        }
        await using (NpgsqlCommand add = new("insert into finance.category(id, kind, scope, user_id, code, name, created_utc, updated_utc) values (@id, @kind, @scope, @user_id, @code, @name, @created_utc, @updated_utc) on conflict do nothing returning id::text, name, coalesce(code, '')", link, lane))
        {
            add.Parameters.AddWithValue("id", Guid.CreateVersion7());
            add.Parameters.AddWithValue("kind", kind);
            add.Parameters.AddWithValue("scope", "user");
            add.Parameters.AddWithValue(UserId, userId);
            add.Parameters.Add("code", NpgsqlDbType.Text).Value = DBNull.Value;
            add.Parameters.AddWithValue("name", text);
            add.Parameters.AddWithValue(CreatedUtc, when);
            add.Parameters.AddWithValue(UpdatedUtc, when);
            await using NpgsqlDataReader row = await add.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), row.GetString(2));
            }
        }
        await using NpgsqlCommand note = new("select id::text, name, coalesce(code, '') from finance.category where kind = @kind and lower(name) = lower(@name) and user_id = @user_id limit 1", link, lane);
        note.Parameters.AddWithValue("kind", kind);
        note.Parameters.AddWithValue("name", text);
        note.Parameters.AddWithValue(UserId, userId);
        await using NpgsqlDataReader data = await note.ExecuteReaderAsync(token);
        if (await data.ReadAsync(token))
        {
            return new PickData(data.GetString(0), data.GetString(1), data.GetString(2));
        }
        throw new InvalidOperationException("Category upsert failed");
    }
    private static async ValueTask<bool> Delete(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string transactionId, string kind, DateTimeOffset when, CancellationToken token)
    {
        Guid itemId = Parse(transactionId, nameof(transactionId));
        await using NpgsqlCommand item = new("select account_id, amount from finance.transaction_entry where id = @id and user_id = @user_id for update", link, lane);
        item.Parameters.AddWithValue("id", itemId);
        item.Parameters.AddWithValue(UserId, userId);
        Guid accountId;
        decimal amount;
        await using (NpgsqlDataReader row = await item.ExecuteReaderAsync(token))
        {
            if (!await row.ReadAsync(token))
            {
                return false;
            }
            accountId = row.GetGuid(0);
            amount = row.GetDecimal(1);
        }
        await using NpgsqlCommand note = new("delete from finance.transaction_entry where id = @id and user_id = @user_id", link, lane);
        note.Parameters.AddWithValue("id", itemId);
        note.Parameters.AddWithValue(UserId, userId);
        if (await note.ExecuteNonQueryAsync(token) != 1)
        {
            return false;
        }
        await using NpgsqlCommand data = new($"update finance.account set current_amount = current_amount {Reverse(kind)} @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
        data.Parameters.AddWithValue("amount", amount);
        data.Parameters.AddWithValue(UpdatedUtc, when);
        data.Parameters.AddWithValue("account_id", accountId);
        data.Parameters.AddWithValue(UserId, userId);
        if (await data.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Account balance update failed");
        }
        return true;
    }
    private static async ValueTask<bool> Recategorize(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CorrectionNote note, DateTimeOffset when, CancellationToken token)
    {
        Guid itemId = Parse(note.TransactionId, nameof(note.TransactionId));
        string categoryId = note.CategoryId;
        if (string.IsNullOrWhiteSpace(categoryId))
        {
            return false;
        }
        await using NpgsqlCommand item = new("update finance.transaction_entry set category_id = @category_id, updated_utc = @updated_utc where id = @id and user_id = @user_id", link, lane);
        item.Parameters.AddWithValue("category_id", Parse(categoryId, nameof(note.CategoryId)));
        item.Parameters.AddWithValue(UpdatedUtc, when);
        item.Parameters.AddWithValue("id", itemId);
        item.Parameters.AddWithValue(UserId, userId);
        return await item.ExecuteNonQueryAsync(token) == 1;
    }
    private static async ValueTask Transaction(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, TransactionNote note, DateTimeOffset when, CancellationToken token)
    {
        Guid accountId = Parse(note.AccountId, nameof(note.AccountId));
        Guid categoryId = Parse(note.CategoryId, nameof(note.CategoryId));
        string kind = Supported(note.TransactionKind);
        string sign = Change(kind);
        await using (NpgsqlCommand item = new("insert into finance.transaction_entry(id, user_id, account_id, category_id, kind, amount, occurred_utc, created_utc, updated_utc) values (@id, @user_id, @account_id, @category_id, @kind, @amount, @occurred_utc, @created_utc, @updated_utc)", link, lane))
        {
            item.Parameters.AddWithValue("id", Guid.CreateVersion7());
            item.Parameters.AddWithValue(UserId, userId);
            item.Parameters.AddWithValue("account_id", accountId);
            item.Parameters.AddWithValue("category_id", categoryId);
            item.Parameters.AddWithValue("kind", kind);
            item.Parameters.AddWithValue("amount", note.Total);
            item.Parameters.AddWithValue("occurred_utc", when);
            item.Parameters.AddWithValue(CreatedUtc, when);
            item.Parameters.AddWithValue(UpdatedUtc, when);
            if (await item.ExecuteNonQueryAsync(token) != 1)
            {
                throw new InvalidOperationException("Transaction insert failed");
            }
        }
        await using NpgsqlCommand data = new($"update finance.account set current_amount = current_amount {sign} @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
        data.Parameters.AddWithValue("amount", note.Total);
        data.Parameters.AddWithValue(UpdatedUtc, when);
        data.Parameters.AddWithValue("account_id", accountId);
        data.Parameters.AddWithValue(UserId, userId);
        if (await data.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Account balance update failed");
        }
    }
    private async ValueTask Outbox<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, WorkspaceItem item, WorkspaceViewNote note, CancellationToken token) where TMessage : class
    {
        var state = new WorkspaceState(item.Snapshot.State, item.Snapshot.Data, item.Snapshot.Revision);
        var view = new WorkspaceView(note.Identity, note.Profile, state, policy.Codes(state.Code, Context(Data(state.Data))), note.IsNewUser, note.IsNewWorkspace, note.When);
        var body = new WorkspaceViewRequestedCommand(view.Identity, view.Profile, new WorkspaceViewFrame(view.State.Code, view.State.Data, view.Actions), new WorkspaceViewFreshness(view.IsNewUser, view.IsNewWorkspace), view.OccurredUtc);
        var envelope = new MessageEnvelope<WorkspaceViewRequestedCommand>(Guid.CreateVersion7(), ViewContract, note.When, new MessageContext(message.Context.CorrelationId, message.MessageId.ToString(), $"{message.Context.IdempotencyKey}:workspace-view"), ViewSource, body);
        string raw = JsonSerializer.Serialize(envelope, json);
        await using NpgsqlCommand itemNote = new("insert into finance.outbox_message(message_id, contract, routing_key, source, correlation_id, causation_id, idempotency_key, payload, occurred_utc, created_utc, published_utc, attempt, error) values (@message_id, @contract, @routing_key, @source, @correlation_id, @causation_id, @idempotency_key, @payload, @occurred_utc, @created_utc, @published_utc, @attempt, @error) on conflict do nothing", link, lane);
        itemNote.Parameters.AddWithValue("message_id", envelope.MessageId);
        itemNote.Parameters.AddWithValue("contract", ViewContract);
        itemNote.Parameters.AddWithValue("routing_key", ViewContract);
        itemNote.Parameters.AddWithValue("source", ViewSource);
        itemNote.Parameters.AddWithValue("correlation_id", envelope.Context.CorrelationId);
        itemNote.Parameters.AddWithValue("causation_id", envelope.Context.CausationId);
        itemNote.Parameters.AddWithValue("idempotency_key", envelope.Context.IdempotencyKey);
        itemNote.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, raw);
        itemNote.Parameters.AddWithValue("occurred_utc", view.OccurredUtc);
        itemNote.Parameters.AddWithValue(CreatedUtc, DateTimeOffset.UtcNow);
        itemNote.Parameters.AddWithValue("published_utc", DBNull.Value);
        itemNote.Parameters.AddWithValue("attempt", 0);
        itemNote.Parameters.AddWithValue("error", string.Empty);
        if (await itemNote.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Outbox insert failed");
        }
    }
    private static async ValueTask Processed<TMessage>(NpgsqlConnection link, NpgsqlTransaction lane, MessageEnvelope<TMessage> message, CancellationToken token) where TMessage : class
    {
        await using NpgsqlCommand note = new("update finance.inbox_message set processed_utc = @processed_utc where contract = @contract and message_id = @message_id", link, lane);
        note.Parameters.AddWithValue("processed_utc", DateTimeOffset.UtcNow);
        note.Parameters.AddWithValue("contract", message.Contract);
        note.Parameters.AddWithValue("message_id", message.MessageId);
        if (await note.ExecuteNonQueryAsync(token) != 1)
        {
            throw new InvalidOperationException("Inbox processed update failed");
        }
    }
    private static Guid Parse(string value, string name) => Guid.TryParse(value, out Guid item) ? item : throw new ArgumentException("Workspace identity value is invalid", name);
    private static async ValueTask<Guid?> Id(NpgsqlCommand note, CancellationToken token)
    {
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        return await row.ReadAsync(token) ? row.GetGuid(0) : null;
    }
    private static async ValueTask<WorkspaceItem?> Item(NpgsqlCommand note, bool isNew, CancellationToken token)
    {
        await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
        return await row.ReadAsync(token) ? new WorkspaceItem(row.GetGuid(0), new WorkspaceSnapshot(row.GetString(1), row.GetString(2), row.GetInt64(3), isNew)) : null;
    }
    private sealed record WorkspaceMove
    {
        internal WorkspaceMove(string code, WorkspaceData body, AccountDraft? account, string category, TransactionNote? transaction, CorrectionNote? correction = null)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            AccountValue = account;
            CategoryEntry = category ?? throw new ArgumentNullException(nameof(category));
            RecordValue = transaction;
            CorrectValue = correction;
        }
        internal string Code { get; }
        internal WorkspaceData Body { get; }
        internal AccountDraft? AccountValue { get; }
        internal string CategoryEntry { get; }
        internal TransactionNote? RecordValue { get; }
        internal CorrectionNote? CorrectValue { get; }
    }
    private sealed record AccountDraft
    {
        internal AccountDraft(string title, string unit, decimal total)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Total = total;
        }
        internal string Title { get; }
        internal string Unit { get; }
        internal decimal Total { get; }
    }
    private sealed record TransactionNote
    {
        internal TransactionNote(string accountId, string categoryId, decimal total, string kind)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
            Total = total;
            TransactionKind = Supported(kind ?? throw new ArgumentNullException(nameof(kind)));
        }
        internal string AccountId { get; }
        internal string CategoryId { get; }
        internal decimal Total { get; }
        internal string TransactionKind { get; }
    }
    private sealed record CorrectionNote
    {
        internal CorrectionNote(string transactionId, string kind, string mode, string categoryId)
        {
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            TransactionKind = Supported(kind ?? throw new ArgumentNullException(nameof(kind)));
            Mode = mode ?? throw new ArgumentNullException(nameof(mode));
            CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
        }
        internal string TransactionId { get; }
        internal string TransactionKind { get; }
        internal string Mode { get; }
        internal string CategoryId { get; }
    }
    private sealed record WorkspaceFrame
    {
        internal WorkspaceFrame(Guid user, string room, string state, string body, string entry, string last, DateTimeOffset when)
        {
            UserValue = user;
            Room = room ?? throw new ArgumentNullException(nameof(room));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            Last = last ?? throw new ArgumentNullException(nameof(last));
            When = when;
        }
        internal Guid UserValue { get; }
        internal string Room { get; }
        internal string State { get; }
        internal string Body { get; }
        internal string Entry { get; }
        internal string Last { get; }
        internal DateTimeOffset When { get; }
    }
    private sealed record WorkspaceMark
    {
        internal WorkspaceMark(Guid id, long revision, WorkspaceFrame frame)
        {
            IdValue = id;
            Revision = revision;
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }
        internal Guid IdValue { get; }
        internal long Revision { get; }
        internal WorkspaceFrame Frame { get; }
    }
    private sealed record WorkspaceWrite
    {
        internal WorkspaceWrite(WorkspaceItem item, bool isNew)
        {
            State = item ?? throw new ArgumentNullException(nameof(item));
            IsNew = isNew;
        }
        internal WorkspaceItem State { get; }
        internal bool IsNew { get; }
    }
    private sealed record WorkspaceViewNote
    {
        internal WorkspaceViewNote(WorkspaceIdentity identity, WorkspaceProfile profile, bool isNewUser, bool isNewWorkspace, DateTimeOffset when)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            IsNewUser = isNewUser;
            IsNewWorkspace = isNewWorkspace;
            When = when;
        }
        internal WorkspaceIdentity Identity { get; }
        internal WorkspaceProfile Profile { get; }
        internal bool IsNewUser { get; }
        internal bool IsNewWorkspace { get; }
        internal DateTimeOffset When { get; }
    }
}
