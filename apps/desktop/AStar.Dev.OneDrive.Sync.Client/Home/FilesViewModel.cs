using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class FilesViewModel(
    IAuthService authService,
    IGraphService graphService,
    IAccountRepository repository) : ObservableObject
{
    public ObservableCollection<Accounts.AccountFilesViewModel> Tabs { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTabs))]
    [NotifyPropertyChangedFor(nameof(HasNoAccounts))]
    public partial Accounts.AccountFilesViewModel? ActiveTab { get; set; }

    public bool HasTabs => Tabs.Count > 0;
    public bool HasNoAccounts => Tabs.Count == 0;

    public event EventHandler<(string AccountId, string FolderId)>? ViewActivityRequested;

    [RelayCommand]
    private async Task ActivateTabAsync(string accountId)
        => await ActivateAccountAsync(accountId);

    public void AddAccount(OneDriveAccount account)
    {
        if(Tabs.Any(t => t.AccountId == account.Id))
            return;

        var tab = new Accounts.AccountFilesViewModel(
            account, authService, graphService, repository);

        tab.ViewActivityRequested += (_, node) =>
            ViewActivityRequested?.Invoke(this,
                (tab.AccountId, FolderId: node.Id));

        Tabs.Add(tab);
        OnPropertyChanged(nameof(HasTabs));
        OnPropertyChanged(nameof(HasNoAccounts));

        if(ActiveTab is null)
            ActivateTab(tab);
    }

    public void RemoveAccount(string accountId)
    {
        var tab = Tabs.FirstOrDefault(t => t.AccountId == accountId);
        if(tab is null)
            return;

        _ = Tabs.Remove(tab);
        OnPropertyChanged(nameof(HasTabs));
        OnPropertyChanged(nameof(HasNoAccounts));

        if(ActiveTab == tab)
            ActivateTab(Tabs.FirstOrDefault());
    }

    public async Task ActivateAccountAsync(string accountId)
    {
        var tab = Tabs.FirstOrDefault(t => t.AccountId == accountId);
        if(tab is null)
            return;

        ActivateTab(tab);
        await tab.LoadCommand.ExecuteAsync(null);
    }

    private void ActivateTab(Accounts.AccountFilesViewModel? tab)
    {
        foreach(var t in Tabs)
            t.IsActiveTab = t == tab;

        ActiveTab = tab;
    }
}
