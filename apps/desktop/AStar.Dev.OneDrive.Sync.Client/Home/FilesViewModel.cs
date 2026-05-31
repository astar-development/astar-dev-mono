using System.Collections.ObjectModel;
using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class FilesViewModel(IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncRuleRepository syncRuleRepository, IFileSystem fileSystem, IFileManagerService fileManagerService, ILogger<AccountFilesViewModel> accountFilesLogger, ILogger<FolderTreeNodeViewModel> folderTreeLogger, ILocalizationService localizationService) : ObservableObject
{
    public ObservableCollection<AccountFilesViewModel> Tabs { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTabs))]
    [NotifyPropertyChangedFor(nameof(HasNoAccounts))]
    public partial AccountFilesViewModel? ActiveTab { get; set; }

    public bool HasTabs => Tabs.Count > 0;
    public bool HasNoAccounts => Tabs.Count == 0;

    public event EventHandler<(string AccountId, string FolderId)>? ViewActivityRequested;

    [RelayCommand]
    private async Task ActivateTabAsync(string accountId)
        => await ActivateAccountAsync(accountId);

    public void AddAccount(OneDriveAccount account)
    {
        if(Tabs.Any(t => t.AccountId == account.Id.Id))
            return;

        var tab = new AccountFilesViewModel(
            account, authService, graphService, repository, syncRuleRepository, fileSystem, fileManagerService, accountFilesLogger, folderTreeLogger, localizationService);

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
