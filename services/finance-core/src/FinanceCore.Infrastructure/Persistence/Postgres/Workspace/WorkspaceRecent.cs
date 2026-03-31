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
            return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(page < 0 ? 0 : page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()), null, string.Empty, null);
        }
        if (code == WorkspaceBody.RecentNext)
        {
            int page = data.Recent.HasNext ? data.Recent.Page + 1 : data.Recent.Page;
            return new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData()), null, string.Empty, null);
        }
        int slot = body.Slot(code, WorkspaceBody.RecentItemSlot);
        RecentItemData item = body.Item(data.Recent.Items, slot);
        return new WorkspaceMove(WorkspaceBody.RecentDetailState, body.Recent(data, new RecentData(data.Recent.Page, data.Recent.HasPrevious, data.Recent.HasNext, data.Recent.Items, new RecentItemData(item.Slot, new RecentEntryData(item.Id, item.Kind, item.Account, item.Category, item.Amount, item.Currency, item.OccurredUtc)))), null, string.Empty, null);
    }

    internal WorkspaceMove Detail(WorkspaceData data, string code) => code switch
    {
        WorkspaceBody.RecentDelete => Selected(data, WorkspaceBody.RecentDeleteState, WorkspaceBody.TransactionMissingNotice),
        WorkspaceBody.RecentRecategorize => Selected(data, WorkspaceBody.RecentCategoryState, WorkspaceBody.TransactionMissingNotice),
        _ => new WorkspaceMove(WorkspaceBody.RecentDetailState, body.Model(data, status: new StatusData("Use the buttons to continue", string.Empty)), null, string.Empty, null)
    };

    internal WorkspaceMove Delete(WorkspaceData data, string code)
    {
        if (code != WorkspaceBody.RecentDeleteApply)
        {
            return new WorkspaceMove(WorkspaceBody.RecentDeleteState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)), null, string.Empty, null);
        }
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(WorkspaceBody.RecentDeleteState, data, null, string.Empty, null, new CorrectionNote(item.Id, item.Kind, WorkspaceBody.DeleteMode, string.Empty));
    }

    internal WorkspaceMove Category(WorkspaceData data, string code)
    {
        int slot = body.Slot(code, WorkspaceBody.RecentCategorySlot);
        OptionData item = body.Option(data.Choices.Categories, slot);
        RecentItemData selected = data.Recent.Selected;
        WorkspaceData state = body.Recent(data, new RecentData(data.Recent.Page, data.Recent.HasPrevious, data.Recent.HasNext, data.Recent.Items, new RecentItemData(selected.Slot, new RecentEntryData(selected.Id, selected.Kind, selected.Account, new PickData(item.Id, item.Name, item.Note), selected.Amount, selected.Currency, selected.OccurredUtc))));
        return new WorkspaceMove(WorkspaceBody.RecentRecategorizeState, state, null, string.Empty, null);
    }

    internal WorkspaceMove Text(WorkspaceData data, string value)
    {
        string text = value.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WorkspaceMove(WorkspaceBody.RecentCategoryState, body.Model(data, status: new StatusData("Category name is required", string.Empty)), null, string.Empty, null);
        }
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(WorkspaceBody.RecentCategoryState, body.Model(data, status: new StatusData()), null, text, null, new CorrectionNote(item.Id, item.Kind, WorkspaceBody.RecategorizeMode, string.Empty));
    }

    internal WorkspaceMove Confirm(WorkspaceData data, string code)
    {
        if (code != WorkspaceBody.RecentRecategorizeApply)
        {
            return new WorkspaceMove(WorkspaceBody.RecentRecategorizeState, body.Model(data, status: new StatusData(WorkspaceBody.ConfirmGoBackPrompt, string.Empty)), null, string.Empty, null);
        }
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Category.Id)
            ? new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, WorkspaceBody.TransactionMissingNotice)), null, string.Empty, null)
            : new WorkspaceMove(WorkspaceBody.RecentRecategorizeState, data, null, string.Empty, null, new CorrectionNote(item.Id, item.Kind, WorkspaceBody.RecategorizeMode, item.Category.Id));
    }

    internal WorkspaceMove Return(WorkspaceData data, string state) => state switch
    {
        WorkspaceBody.RecentListState => new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, WorkspaceBody.ChooseActionPrompt), null, string.Empty, null),
        WorkspaceBody.RecentDetailState => new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, data.Recent.HasPrevious, data.Recent.HasNext, data.Recent.Items, new RecentItemData())), null, string.Empty, null),
        WorkspaceBody.RecentDeleteState => new WorkspaceMove(WorkspaceBody.RecentDetailState, data, null, string.Empty, null),
        WorkspaceBody.RecentCategoryState => new WorkspaceMove(WorkspaceBody.RecentDetailState, body.Recent(data, data.Recent, new ChoicesData(), new StatusData()), null, string.Empty, null),
        WorkspaceBody.RecentRecategorizeState => new WorkspaceMove(WorkspaceBody.RecentDetailState, data, null, string.Empty, null),
        _ => new WorkspaceMove(WorkspaceBody.HomeState, body.Home(data.Accounts, data.Accounts.Count == 0 ? WorkspaceBody.AddAccountPrompt : WorkspaceBody.ChooseActionPrompt), null, string.Empty, null)
    };

    private WorkspaceMove Selected(WorkspaceData data, string state, string notice)
    {
        RecentItemData item = data.Recent.Selected;
        return string.IsNullOrWhiteSpace(item.Id)
            ? new WorkspaceMove(WorkspaceBody.RecentListState, body.Recent(data, new RecentData(data.Recent.Page, false, false, [], new RecentItemData()), new ChoicesData(), new StatusData(string.Empty, notice)), null, string.Empty, null)
            : new WorkspaceMove(state, data, null, string.Empty, null);
    }
}
