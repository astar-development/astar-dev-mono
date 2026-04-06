using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class FilesViewModel(
    IAuthService authService,
    IGraphService graphService,
    IAccountRepository repository) : ObservableObject
{
    public ObservableCollection<AccountFilesViewModel> Tabs { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTabs))]
    [NotifyPropertyChangedFor(nameof(HasNoAccounts))]
    private AccountFilesViewModel? _activeTab;

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

        var tab = new AccountFilesViewModel(
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

    private void ActivateTab(AccountFilesViewModel? tab)
    {
        foreach(var t in Tabs)
            t.IsActiveTab = t == tab;

        ActiveTab = tab;
    }
}
