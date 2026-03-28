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
    private const string HomeState = "home";
    private const string NameState = "account.name";
    private const string CurrencyState = "account.currency";
    private const string BalanceState = "account.balance";
    private const string ConfirmState = "account.confirm";
    private const string ExpenseAccountState = "transaction.expense.account";
    private const string ExpenseAmountState = "transaction.expense.amount";
    private const string ExpenseCategoryState = "transaction.expense.category";
    private const string ExpenseConfirmState = "transaction.expense.confirm";
    private const string AddAccount = "account.add";
    private const string AddExpense = "transaction.expense.add";
    private const string AccountCancel = "account.cancel";
    private const string ExpenseCancel = "transaction.expense.cancel";
    private const string CreateAccountCode = "account.create";
    private const string CreateExpenseCode = "transaction.expense.create";
    private const string Rub = "account.currency.rub";
    private const string Usd = "account.currency.usd";
    private const string Eur = "account.currency.eur";
    private const string Other = "account.currency.other";
    private const string AccountSlot = "transaction.expense.account.";
    private const string CategorySlot = "transaction.expense.category.";
    private const string ExpenseKind = "expense";
    private const string ViewContract = "workspace.view.requested";
    private const string ViewSource = "finance-core";
    private const string CreatedUtc = "created_utc";
    private const string UpdatedUtc = "updated_utc";
    private const string UserId = "user_id";
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
            WorkspaceItem? current = await Read(link, lane, command.Identity.ConversationKey, userId, token);
            IReadOnlyList<AccountData> list = await Accounts(link, lane, userId, token);
            WorkspaceData body = current is null ? Home(list, string.Empty) : Data(current.Snapshot.Data);
            string state = current?.Snapshot.State ?? HomeState;
            WorkspaceMove move = Move(state, body, command);
            if (!string.IsNullOrWhiteSpace(move.CategoryValue))
            {
                move = await CategoryPick(link, lane, userId, move, when, token);
            }
            if (move.AccountValue is not null)
            {
                bool fresh = await Account(link, lane, userId, move.AccountValue, when, token);
                if (!fresh)
                {
                    move = new WorkspaceMove(NameState, new WorkspaceData(body.Accounts, new FinancialData(move.AccountValue.Title, move.AccountValue.Unit, move.AccountValue.Total), new ExpenseData(), new ChoicesData(), new StatusData("Account name already exists", string.Empty), false), null, string.Empty, null);
                }
            }
            if (move.Code == ExpenseCategoryState && move.Body.Choices.Categories.Count == 0)
            {
                IReadOnlyList<OptionData> categories = await Categories(link, lane, userId, token);
                move = new WorkspaceMove(ExpenseCategoryState, new WorkspaceData(move.Body.Accounts, move.Body.Financial, move.Body.Expense, new ChoicesData([], categories), move.Body.Status, false), null, string.Empty, null);
            }
            if (move.ExpenseValue is not null)
            {
                await Expense(link, lane, userId, move.ExpenseValue, when, token);
                move = new WorkspaceMove(HomeState, Home(await Accounts(link, lane, userId, token), "Expense was recorded"), null, string.Empty, null);
            }
            if (move.Code == HomeState)
            {
                move = new WorkspaceMove(HomeState, Home(await Accounts(link, lane, userId, token), move.Body.Status.Notice), null, string.Empty, null);
            }
            string note = Json(move.Body);
            var frame = new WorkspaceFrame(userId, command.Identity.ConversationKey, move.Code, note, string.Empty, command.Value, when);
            WorkspaceItem? next = current is null ? await Add(link, lane, frame, token) : await Write(link, lane, new WorkspaceMark(current.Id, current.Snapshot.Revision, frame), token);
            if (next is not null)
            {
                return new WorkspaceWrite(next, current is null);
            }
        }
        throw new InvalidOperationException($"Workspace input exceeded retry limit for conversation '{command.Identity.ConversationKey}'");
    }
    private static WorkspaceMove Move(string state, WorkspaceData body, WorkspaceInputRequestedCommand command)
    {
        string kind = command.Kind.Trim();
        return kind switch
        {
            "action" => Act(state, body, command.Value),
            "text" => Text(state, body, command.Value),
            _ => new WorkspaceMove(state, new WorkspaceData(body.Accounts, body.Financial, body.Expense, body.Choices, new StatusData("Input kind is not supported", body.Status.Notice), body.Custom), null, string.Empty, null)
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
        return state switch
        {
            HomeState => HomeAction(body, code),
            CurrencyState => AccountCurrency(body, code),
            ConfirmState => AccountConfirm(body, code),
            ExpenseAccountState => ExpenseAccount(body, code),
            ExpenseCategoryState => CategoryAction(body, code),
            ExpenseConfirmState => ExpenseConfirm(body, code),
            _ => new WorkspaceMove(state, new WorkspaceData(body.Accounts, body.Financial, body.Expense, body.Choices, new StatusData("This action is not available", string.Empty), body.Custom), null, string.Empty, null)
        };
    }
    private static WorkspaceMove Text(string state, WorkspaceData body, string value) => state switch
    {
        HomeState when body.Accounts.Count == 0 => new WorkspaceMove(HomeState, Home(body.Accounts, "Tap Add account to start"), null, string.Empty, null),
        HomeState => new WorkspaceMove(HomeState, Home(body.Accounts, "Choose one action"), null, string.Empty, null),
        NameState => Draft(body, value),
        CurrencyState => Currency(body, value),
        BalanceState => Amount(body, value),
        ConfirmState => new WorkspaceMove(ConfirmState, new WorkspaceData(body.Accounts, body.Financial, new ExpenseData(), new ChoicesData(), new StatusData("Use the buttons to confirm or cancel", string.Empty), false), null, string.Empty, null),
        ExpenseAccountState => new WorkspaceMove(ExpenseAccountState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, body.Choices, new StatusData("Use the buttons to choose one account or cancel", string.Empty), false), null, string.Empty, null),
        ExpenseAmountState => ExpenseAmount(body, value),
        ExpenseCategoryState => CategoryText(body, value),
        ExpenseConfirmState => new WorkspaceMove(ExpenseConfirmState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, new ChoicesData(), new StatusData("Use the buttons to confirm or cancel", string.Empty), false), null, string.Empty, null),
        _ => new WorkspaceMove(HomeState, Home(body.Accounts, body.Accounts.Count == 0 ? "Tap Add account to start" : "Choose one action"), null, string.Empty, null)
    };
    private static WorkspaceMove HomeAction(WorkspaceData body, string code)
    {
        if (code == AddAccount)
        {
            return new WorkspaceMove(NameState, new WorkspaceData(body.Accounts, new FinancialData(string.Empty, string.Empty, null), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
        }
        if (code == AddExpense)
        {
            return ExpenseStart(body);
        }
        return new WorkspaceMove(HomeState, Home(body.Accounts, body.Accounts.Count == 0 ? "Tap Add account to start" : "Choose one action"), null, string.Empty, null);
    }
    private static WorkspaceMove AccountCurrency(WorkspaceData body, string code) => code switch
    {
        Rub => Currency(body, "RUB"),
        Usd => Currency(body, "USD"),
        Eur => Currency(body, "EUR"),
        Other => new WorkspaceMove(CurrencyState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, string.Empty, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData(), true), null, string.Empty, null),
        _ => new WorkspaceMove(CurrencyState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Choose one currency option or send a 3 letter code", string.Empty), body.Custom), null, string.Empty, null)
    };
    private static WorkspaceMove AccountConfirm(WorkspaceData body, string code) => code == CreateAccountCode ? Create(body) : new WorkspaceMove(ConfirmState, new WorkspaceData(body.Accounts, body.Financial, new ExpenseData(), new ChoicesData(), new StatusData("Confirm the account or cancel", string.Empty), false), null, string.Empty, null);
    private static WorkspaceMove ExpenseStart(WorkspaceData body)
    {
        if (body.Accounts.Count == 0)
        {
            return new WorkspaceMove(HomeState, Home(body.Accounts, "Add an account to record an expense"), null, string.Empty, null);
        }
        if (body.Accounts.Count == 1)
        {
            AccountData item = body.Accounts[0];
            return new WorkspaceMove(ExpenseAmountState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(new PickData(item.Id, item.Name, item.Currency), new PickData(), null), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
        }
        return new WorkspaceMove(ExpenseAccountState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(), new ChoicesData(AccountChoices(body.Accounts), []), new StatusData(), false), null, string.Empty, null);
    }
    private static WorkspaceMove ExpenseAccount(WorkspaceData body, string code)
    {
        int slot = Slot(code, AccountSlot);
        OptionData? item = Option(body.Choices.Accounts, slot);
        return item is null
            ? new WorkspaceMove(ExpenseAccountState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, body.Choices, new StatusData("Choose one account", string.Empty), false), null, string.Empty, null)
            : new WorkspaceMove(ExpenseAmountState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(new PickData(item.Id, item.Name, item.Note), new PickData(), null), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
    }
    private static WorkspaceMove CategoryAction(WorkspaceData body, string code)
    {
        int slot = Slot(code, CategorySlot);
        OptionData? item = Option(body.Choices.Categories, slot);
        return item is null
            ? new WorkspaceMove(ExpenseCategoryState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, body.Choices, new StatusData("Choose one category or send a new name", string.Empty), false), null, string.Empty, null)
            : new WorkspaceMove(ExpenseConfirmState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(body.Expense.Account, new PickData(item.Id, item.Name, item.Note), body.Expense.Amount), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
    }
    private static WorkspaceMove ExpenseConfirm(WorkspaceData body, string code) => code == CreateExpenseCode ? Create(body) : new WorkspaceMove(ExpenseConfirmState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, new ChoicesData(), new StatusData("Confirm the expense or cancel", string.Empty), false), null, string.Empty, null);
    private static WorkspaceMove Draft(WorkspaceData body, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WorkspaceMove(NameState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Account name is required", string.Empty), false), null, string.Empty, null);
        }
        if (body.Financial.Amount.HasValue && !string.IsNullOrWhiteSpace(body.Financial.Currency))
        {
            return new WorkspaceMove(ConfirmState, new WorkspaceData(body.Accounts, new FinancialData(text, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
        }
        if (!string.IsNullOrWhiteSpace(body.Financial.Currency))
        {
            return new WorkspaceMove(BalanceState, new WorkspaceData(body.Accounts, new FinancialData(text, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
        }
        return new WorkspaceMove(CurrencyState, new WorkspaceData(body.Accounts, new FinancialData(text, string.Empty, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
    }
    private static WorkspaceMove Currency(WorkspaceData body, string value)
    {
        string text = value.Trim().ToUpperInvariant();
        bool valid = text.Length == 3 && text.All(char.IsLetter);
        if (!valid)
        {
            return new WorkspaceMove(CurrencyState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Currency code must contain 3 letters", string.Empty), true), null, string.Empty, null);
        }
        return body.Financial.Amount.HasValue
            ? new WorkspaceMove(ConfirmState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, text, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null)
            : new WorkspaceMove(BalanceState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, text, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
    }
    private static WorkspaceMove Amount(WorkspaceData body, string value)
    {
        string text = value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("\u00A0", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        bool ok = decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount);
        return ok
            ? new WorkspaceMove(ConfirmState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, amount), new ExpenseData(), new ChoicesData(), new StatusData(), false), null, string.Empty, null)
            : new WorkspaceMove(BalanceState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Balance must be a number", string.Empty), false), null, string.Empty, null);
    }
    private static WorkspaceMove ExpenseAmount(WorkspaceData body, string value)
    {
        string text = value.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("\u00A0", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        bool ok = decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount);
        if (!ok || amount <= 0m)
        {
            return new WorkspaceMove(ExpenseAmountState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(body.Expense.Account, new PickData(), body.Expense.Amount), new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty), false), null, string.Empty, null);
        }
        return new WorkspaceMove(ExpenseCategoryState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(body.Expense.Account, new PickData(), amount), new ChoicesData(), new StatusData(), false), null, string.Empty, null);
    }
    private static WorkspaceMove CategoryText(WorkspaceData body, string value)
    {
        string text = value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? new WorkspaceMove(ExpenseCategoryState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, body.Choices, new StatusData("Category name is required", string.Empty), false), null, string.Empty, null)
            : new WorkspaceMove(ExpenseCategoryState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, body.Choices, new StatusData(), false), null, text, null);
    }
    private static WorkspaceMove Create(WorkspaceData body)
    {
        if (!string.IsNullOrWhiteSpace(body.Financial.Name) || !string.IsNullOrWhiteSpace(body.Financial.Currency) || body.Financial.Amount.HasValue)
        {
            return AccountCreate(body);
        }
        return ExpenseCreate(body);
    }
    private static WorkspaceMove AccountCreate(WorkspaceData body)
    {
        if (string.IsNullOrWhiteSpace(body.Financial.Name))
        {
            return new WorkspaceMove(NameState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Account name is required", string.Empty), false), null, string.Empty, null);
        }
        if (string.IsNullOrWhiteSpace(body.Financial.Currency))
        {
            return new WorkspaceMove(CurrencyState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Currency code must contain 3 letters", string.Empty), true), null, string.Empty, null);
        }
        if (!body.Financial.Amount.HasValue)
        {
            return new WorkspaceMove(BalanceState, new WorkspaceData(body.Accounts, new FinancialData(body.Financial.Name, body.Financial.Currency, body.Financial.Amount), new ExpenseData(), new ChoicesData(), new StatusData("Balance must be a number", string.Empty), false), null, string.Empty, null);
        }
        return new WorkspaceMove(HomeState, Reset(body, "Account was created"), new AccountDraft(body.Financial.Name, body.Financial.Currency, body.Financial.Amount.Value), string.Empty, null);
    }
    private static WorkspaceMove ExpenseCreate(WorkspaceData body)
    {
        if (string.IsNullOrWhiteSpace(body.Expense.Account.Id))
        {
            return ExpenseStart(body);
        }
        if (!body.Expense.Amount.HasValue || body.Expense.Amount.Value <= 0m)
        {
            return new WorkspaceMove(ExpenseAmountState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(body.Expense.Account, new PickData(), body.Expense.Amount), new ChoicesData(), new StatusData("Amount must be greater than zero", string.Empty), false), null, string.Empty, null);
        }
        if (string.IsNullOrWhiteSpace(body.Expense.Category.Id))
        {
            return new WorkspaceMove(ExpenseCategoryState, new WorkspaceData(body.Accounts, new FinancialData(), new ExpenseData(body.Expense.Account, new PickData(), body.Expense.Amount), new ChoicesData(), new StatusData("Choose one category or send a new name", string.Empty), false), null, string.Empty, null);
        }
        return new WorkspaceMove(ExpenseConfirmState, new WorkspaceData(body.Accounts, new FinancialData(), body.Expense, new ChoicesData(), new StatusData(), false), null, string.Empty, new ExpenseNote(body.Expense.Account.Id, body.Expense.Category.Id, body.Expense.Amount.Value));
    }
    private static WorkspaceData Reset(WorkspaceData body, string notice) => new(body.Accounts, new FinancialData(), new ExpenseData(), new ChoicesData(), new StatusData(string.Empty, notice), false);
    private static WorkspaceData Home(IReadOnlyList<AccountData> list, string notice) => new(list, new FinancialData(), new ExpenseData(), new ChoicesData(), new StatusData(string.Empty, notice), false);
    private static WorkspaceData Data(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new WorkspaceData();
        }
        WorkspaceData? item = JsonSerializer.Deserialize<WorkspaceData>(value, json);
        return new WorkspaceData(item?.Accounts ?? [], item?.Financial ?? new FinancialData(), item?.Expense ?? new ExpenseData(), item?.Choices ?? new ChoicesData(), item?.Status ?? new StatusData(), item?.Custom ?? false);
    }
    private static string Json(WorkspaceData item) => JsonSerializer.Serialize(item, json);
    private static WorkspaceActionContext Context(WorkspaceData body) => new(body.Accounts.Count, body.Choices.Accounts.Count, body.Choices.Categories.Count, body.Custom);
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
    private static bool AccountState(string state) => state is NameState or CurrencyState or BalanceState or ConfirmState;
    private static bool ExpenseState(string state) => state is ExpenseAccountState or ExpenseAmountState or ExpenseCategoryState or ExpenseConfirmState;
    private static OptionData? Option(IReadOnlyList<OptionData> list, int slot) => list.SingleOrDefault(item => item.Slot == slot);
    private static IReadOnlyList<OptionData> AccountChoices(IReadOnlyList<AccountData> list) => [.. list.Select((item, index) => new OptionData(index + 1, item.Id, item.Name, item.Currency))];
    private static async ValueTask<WorkspaceMove> CategoryPick(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, WorkspaceMove move, DateTimeOffset when, CancellationToken token)
    {
        PickData item = await Category(link, lane, userId, move.CategoryValue, when, token);
        WorkspaceData body = new(move.Body.Accounts, new FinancialData(), new ExpenseData(move.Body.Expense.Account, item, move.Body.Expense.Amount), new ChoicesData(), new StatusData(), false);
        return new WorkspaceMove(ExpenseConfirmState, body, null, string.Empty, null);
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
    private static async ValueTask<IReadOnlyList<OptionData>> Categories(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, CancellationToken token)
    {
        List<OptionData> list = [];
        await using (NpgsqlCommand note = new("select id::text, name from finance.category where kind = @kind and scope = 'system' order by case code when 'food' then 1 when 'transport' then 2 when 'home' then 3 when 'health' then 4 when 'shopping' then 5 when 'fun' then 6 when 'bills' then 7 when 'travel' then 8 else 999 end, name", link, lane))
        {
            note.Parameters.AddWithValue("kind", ExpenseKind);
            await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
            while (await row.ReadAsync(token))
            {
                list.Add(new OptionData(list.Count + 1, row.GetString(0), row.GetString(1), string.Empty));
            }
        }
        await using (NpgsqlCommand note = new("select c.id::text, c.name from finance.category c join (select category_id, max(occurred_utc) as occurred_utc from finance.transaction_entry where user_id = @user_id and kind = @kind group by category_id) t on t.category_id = c.id where c.user_id = @user_id and c.kind = @kind and c.scope = 'user' order by t.occurred_utc desc, c.name limit 6", link, lane))
        {
            note.Parameters.AddWithValue(UserId, userId);
            note.Parameters.AddWithValue("kind", ExpenseKind);
            await using NpgsqlDataReader row = await note.ExecuteReaderAsync(token);
            while (await row.ReadAsync(token))
            {
                list.Add(new OptionData(list.Count + 1, row.GetString(0), row.GetString(1), string.Empty));
            }
        }
        return list;
    }
    private static async ValueTask<PickData> Category(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, string value, DateTimeOffset when, CancellationToken token)
    {
        string text = value.Trim();
        await using (NpgsqlCommand find = new("select id::text, name from finance.category where kind = @kind and lower(name) = lower(@name) and (scope = 'system' or user_id = @user_id) order by case scope when 'system' then 0 else 1 end limit 1", link, lane))
        {
            find.Parameters.AddWithValue("kind", ExpenseKind);
            find.Parameters.AddWithValue("name", text);
            find.Parameters.AddWithValue(UserId, userId);
            await using NpgsqlDataReader row = await find.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), string.Empty);
            }
        }
        await using (NpgsqlCommand add = new("insert into finance.category(id, kind, scope, user_id, code, name, created_utc, updated_utc) values (@id, @kind, @scope, @user_id, @code, @name, @created_utc, @updated_utc) on conflict do nothing returning id::text, name", link, lane))
        {
            add.Parameters.AddWithValue("id", Guid.CreateVersion7());
            add.Parameters.AddWithValue("kind", ExpenseKind);
            add.Parameters.AddWithValue("scope", "user");
            add.Parameters.AddWithValue(UserId, userId);
            add.Parameters.Add("code", NpgsqlDbType.Text).Value = DBNull.Value;
            add.Parameters.AddWithValue("name", text);
            add.Parameters.AddWithValue(CreatedUtc, when);
            add.Parameters.AddWithValue(UpdatedUtc, when);
            await using NpgsqlDataReader row = await add.ExecuteReaderAsync(token);
            if (await row.ReadAsync(token))
            {
                return new PickData(row.GetString(0), row.GetString(1), string.Empty);
            }
        }
        await using NpgsqlCommand note = new("select id::text, name from finance.category where kind = @kind and lower(name) = lower(@name) and user_id = @user_id limit 1", link, lane);
        note.Parameters.AddWithValue("kind", ExpenseKind);
        note.Parameters.AddWithValue("name", text);
        note.Parameters.AddWithValue(UserId, userId);
        await using NpgsqlDataReader data = await note.ExecuteReaderAsync(token);
        if (await data.ReadAsync(token))
        {
            return new PickData(data.GetString(0), data.GetString(1), string.Empty);
        }
        throw new InvalidOperationException("Category upsert failed");
    }
    private static async ValueTask Expense(NpgsqlConnection link, NpgsqlTransaction lane, Guid userId, ExpenseNote note, DateTimeOffset when, CancellationToken token)
    {
        Guid accountId = Parse(note.AccountId, nameof(note.AccountId));
        Guid categoryId = Parse(note.CategoryId, nameof(note.CategoryId));
        await using (NpgsqlCommand item = new("insert into finance.transaction_entry(id, user_id, account_id, category_id, kind, amount, occurred_utc, created_utc, updated_utc) values (@id, @user_id, @account_id, @category_id, @kind, @amount, @occurred_utc, @created_utc, @updated_utc)", link, lane))
        {
            item.Parameters.AddWithValue("id", Guid.CreateVersion7());
            item.Parameters.AddWithValue(UserId, userId);
            item.Parameters.AddWithValue("account_id", accountId);
            item.Parameters.AddWithValue("category_id", categoryId);
            item.Parameters.AddWithValue("kind", ExpenseKind);
            item.Parameters.AddWithValue("amount", note.Total);
            item.Parameters.AddWithValue("occurred_utc", when);
            item.Parameters.AddWithValue(CreatedUtc, when);
            item.Parameters.AddWithValue(UpdatedUtc, when);
            if (await item.ExecuteNonQueryAsync(token) != 1)
            {
                throw new InvalidOperationException("Expense insert failed");
            }
        }
        await using NpgsqlCommand data = new("update finance.account set current_amount = current_amount - @amount, updated_utc = @updated_utc where id = @account_id and user_id = @user_id", link, lane);
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
        internal WorkspaceMove(string code, WorkspaceData body, AccountDraft? account, string category, ExpenseNote? expense)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Body = body ?? throw new ArgumentNullException(nameof(body));
            AccountValue = account;
            CategoryValue = category ?? throw new ArgumentNullException(nameof(category));
            ExpenseValue = expense;
        }
        internal string Code { get; }
        internal WorkspaceData Body { get; }
        internal AccountDraft? AccountValue { get; }
        internal string CategoryValue { get; }
        internal ExpenseNote? ExpenseValue { get; }
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
    private sealed record ExpenseNote
    {
        internal ExpenseNote(string accountId, string categoryId, decimal total)
        {
            AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
            CategoryId = categoryId ?? throw new ArgumentNullException(nameof(categoryId));
            Total = total;
        }
        internal string AccountId { get; }
        internal string CategoryId { get; }
        internal decimal Total { get; }
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
