namespace FinanceCore.Infrastructure.Persistence.Postgres.Workspace;

internal sealed class WorkspaceRecent
{
    private readonly WorkspaceBody body;

    internal WorkspaceRecent(WorkspaceBody body) => this.body = body ?? throw new ArgumentNullException(nameof(body));

    internal WorkspaceMove List(WorkspaceData data, string code)
    {
        if (code == WorkspaceBody.RecentPrevious)
        {
            int page = data.Recent.HasPrevious ? data.Recent.Page - 1 : data.Recent.Page;
            return Move(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(page < 0 ? 0 : page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()));
        }
        if (code == WorkspaceBody.RecentNext)
        {
            int page = data.Recent.HasNext ? data.Recent.Page + 1 : data.Recent.Page;
            return Move(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()));
        }
        int slot = body.Slot(code, WorkspaceBody.RecentItemSlot);
        RecentItemData item = body.Item(data.Recent.Items, slot);
        return Move(WorkspaceBody.RecentDetailState, body.Recent(data, new RecentData(data.Recent.Page, data.Recent.HasPrevious, data.Recent.HasNext, data.Recent.Items, new RecentItemData(item.Slot, new RecentEntryData(item.Id, item.Kind, item.Account, item.Category, item.Amount, item.Currency, item.OccurredUtc)))));
    }

    internal WorkspaceMove Detail(WorkspaceData data, string code) => code switch
    {
        WorkspaceBody.RecentDelete => Selected(data, WorkspaceBody.RecentDeleteState, WorkspaceBody.TransactionMissingNotice),
        WorkspaceBody.RecentRecategorize => Selected(data, WorkspaceBody.RecentCategoryState, WorkspaceBody.TransactionMissingNotice),
        _ => Move(WorkspaceBody.RecentDetailState, body.Model(data, status: new StatusData("Use the buttons to continue", string.Empty)))
    };

    internal WorkspaceMove Delete(WorkspaceData data, string code)
    {
        if (code != WorkspaceBody.RecentDeleteApply)
        {
            return Move(WorkspaceBody.RecentDeleteState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)));
        }
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? Missing(data, WorkspaceBody.TransactionMissingNotice)
            : Move(WorkspaceBody.RecentDeleteState, data, new CorrectionNote(item.Id, item.Kind, WorkspaceBody.DeleteMode, string.Empty));
    }

    internal WorkspaceMove Category(WorkspaceData data, string code)
    {
        int slot = body.Slot(code, WorkspaceBody.RecentCategorySlot);
        OptionData item = body.Option(data.Choices.Categories, slot);
        RecentItemData selected = data.Recent.Selected;
        WorkspaceData state = body.Recent(data, new RecentData(data.Recent.Page, data.Recent.HasPrevious, data.Recent.HasNext, data.Recent.Items, new RecentItemData(selected.Slot, new RecentEntryData(selected.Id, selected.Kind, selected.Account, new PickData(item.Id, item.Name, item.Note), selected.Amount, selected.Currency, selected.OccurredUtc))));
        return Move(WorkspaceBody.RecentRecategorizeState, state);
    }

    internal WorkspaceMove Text(WorkspaceData data, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return Move(WorkspaceBody.RecentCategoryState, body.Model(data, status: new StatusData("Category name is required", string.Empty)));
        }
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? Missing(data, WorkspaceBody.TransactionMissingNotice)
            : Move(WorkspaceBody.RecentCategoryState, body.Model(data, status: new StatusData()), text, new CorrectionNote(item.Id, item.Kind, WorkspaceBody.RecategorizeMode, string.Empty));
    }

    internal WorkspaceMove Confirm(WorkspaceData data, string code)
    {
        if (code != WorkspaceBody.RecentRecategorizeApply)
        {
            return Move(WorkspaceBody.RecentRecategorizeState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)));
        }
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Category.Id)
            ? Missing(data, WorkspaceBody.TransactionMissingNotice)
            : Move(WorkspaceBody.RecentRecategorizeState, data, new CorrectionNote(item.Id, item.Kind, WorkspaceBody.RecategorizeMode, item.Category.Id));
    }

    internal WorkspaceMove Return(WorkspaceData data, string state) => state switch
    {
        WorkspaceBody.RecentListState => Move(WorkspaceBody.HomeState, body.Home(data.Accounts, WorkspaceBody.ChooseActionPrompt)),
        WorkspaceBody.RecentDetailState => Move(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, data.Recent.HasPrevious, data.Recent.HasNext, data.Recent.Items, new RecentItemData()))),
        WorkspaceBody.RecentDeleteState => Move(WorkspaceBody.RecentDetailState, Detail(data)),
        WorkspaceBody.RecentCategoryState => Move(WorkspaceBody.RecentDetailState, Detail(data)),
        WorkspaceBody.RecentRecategorizeState => Move(WorkspaceBody.RecentDetailState, Detail(data)),
        _ => Move(WorkspaceBody.HomeState, body.Home(data.Accounts, data.Accounts.Count == 0 ? WorkspaceBody.AddAccountPrompt : WorkspaceBody.ChooseActionPrompt))
    };

    private WorkspaceMove Selected(WorkspaceData data, string state, string notice)
    {
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? Missing(data, notice)
            : Move(state, data);
    }

    private WorkspaceData Detail(WorkspaceData data) => body.Recent(data, data.Recent, new ChoicesData(), new StatusData());

    private WorkspaceMove Missing(WorkspaceData data, string notice) => Move(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, notice)));

    private static WorkspaceMove Move(string code, WorkspaceData data) => new(code, data);

    private static WorkspaceMove Move(string code, WorkspaceData data, string category, CorrectionNote note) => new(code, data, category, note);

    private static WorkspaceMove Move(string code, WorkspaceData data, CorrectionNote note) => new(code, data, note);
}
