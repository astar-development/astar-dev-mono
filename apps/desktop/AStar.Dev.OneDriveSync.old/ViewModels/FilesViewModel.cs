using System.Collections.ObjectModel;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class FilesViewModel : ReactiveObject
{
    public ObservableCollection<AccountFilesViewModel> Tabs { get; } = [];
    public bool HasNoAccounts => Tabs.Count == 0;
    public bool HasTabs => Tabs.Count > 0;

    public AccountFilesViewModel? ActiveTab
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Task ActivateAccountAsync(string accountId)
    {
        AccountFilesViewModel? tab = Tabs.FirstOrDefault(t => t.AccountId == accountId);
        if(tab is null)
            return Task.CompletedTask;

        foreach(AccountFilesViewModel t in Tabs)
            t.IsActiveTab = t.AccountId == accountId;

        ActiveTab = tab;
        return tab.ActivateAsync();
    }
}
