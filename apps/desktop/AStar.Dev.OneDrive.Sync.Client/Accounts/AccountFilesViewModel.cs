using System.Collections.ObjectModel;
using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Rules;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.Utilities;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using FolderTreeNodeViewModel = AStar.Dev.OneDrive.Sync.Client.Home.FolderTreeNodeViewModel;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncRuleService syncRuleService, IFileSystem fileSystem, IFileManagerService fileManagerService, ILogger<AccountFilesViewModel> logger, IFolderTreeNodeViewModelFactory folderTreeNodeViewModelFactory, ILocalizationService localizationService) : ObservableObject
{
    private readonly OneDriveAccount account = account;
    private readonly IAuthService authService = authService;
    private readonly IGraphService graphService = graphService;
    private readonly IAccountRepository repository = repository;
    private readonly ISyncRuleService syncRuleService = syncRuleService;
    private readonly IFileManagerService fileManagerService = fileManagerService;
    private readonly ILogger<AccountFilesViewModel> logger = logger;
    private readonly IFolderTreeNodeViewModelFactory folderTreeNodeViewModelFactory = folderTreeNodeViewModelFactory;
    private readonly ILocalizationService localizationService = localizationService;
    private string? accessToken;
    private Option<DriveId> driveIdOption = DriveIdFactory.Empty;

    /// <summary>The unique identifier for the account.</summary>
    public string AccountId => account.Id.Id;
    public string DisplayName => account.Profile.DisplayName;
    public string Email => account.Profile.Email;

    public string TabLabel => account.Profile.DisplayName.Length > 0
                                 ? account.Profile.DisplayName
                                 : account.Profile.Email;

    public int AccentIndex => account.AccentIndex;

    public Color AccentColor => AccountCardViewModel.PaletteColor(account.AccentIndex);

    public ObservableCollection<FolderTreeNodeViewModel> RootFolders { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string LoadError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial bool IsActiveTab { get; set; }

    /// <summary>Localised "Loading folders ..." indicator text.</summary>
    public string LoadingFoldersText => localizationService.GetLocal("Files.LoadingFolders");

    /// <summary>Localised "Could not load folders" error heading.</summary>
    public string CouldNotLoadText => localizationService.GetLocal("Files.CouldNotLoad");

    /// <summary>Raised after a folder is included or excluded; the argument is the new count of included rules for this account.</summary>
    public event EventHandler<int>? FolderCountChanged;

    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        HasLoadError = false;
        LoadError = string.Empty;
        RootFolders.Clear();

        try
        {
            accessToken = await authService.AcquireTokenSilentAsync(account.Id.Id)
                .MatchAsync<AuthResult, AuthError, string?>(
                    ok => ok.AccessToken,
                    error =>
                    {
                        LoadError = error is AuthFailedError failed ? failed.Message : "Authentication failed.";
                        HasLoadError = true;
                        return null;
                    });

            if (accessToken is null)
                return;

            var driveId = await graphService.GetDriveIdAsync(account.Id.Id, _ => Task.FromResult(accessToken ?? string.Empty))
                .MatchAsync<DriveId, string, DriveId?>(
                    id => id,
                    error =>
                    {
                        LoadError = $"Failed to retrieve drive ID: {error}";
                        HasLoadError = true;
                        return null;
                    });

            if (driveId is null)
                return;

            driveIdOption = new Option<DriveId>.Some(driveId.Value);

            var includedPaths = await syncRuleService.GetIncludedPathsAsync(account.Id, CancellationToken.None);
            await BuildRootFoldersAsync(includedPaths);
        }
        catch (Exception ex)
        {
            LoadError = $"Failed to load folders: {ex.Message}";
            HasLoadError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildRootFoldersAsync(IReadOnlySet<string> includedPaths)
    {
        var folders = await graphService.GetRootFoldersAsync(account.Id.Id, _ => Task.FromResult(accessToken ?? string.Empty))
            .MatchAsync<List<DriveFolder>, string, List<DriveFolder>?>(
                f => f,
                error =>
                {
                    OneDriveSyncClientMessages.RootFoldersLoadFailed(logger, account.Id.Id, error);
                    LoadError = error;
                    HasLoadError = true;
                    return null;
                });

        if (folders is null)
            return;

        var driveId = driveIdOption.Match<DriveId?>(
            id => id,
            () =>
            {
                OneDriveSyncClientMessages.DriveIdNotAvailable(logger, account.Id.Id);
                return null;
            });

        if (driveId is null)
            return;

        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult(accessToken ?? string.Empty);

        foreach (var f in folders)
        {
            string remotePath = $"/{f.Name}";
            var syncState = includedPaths.Contains(remotePath)
                ? FolderSyncState.Included
                : FolderSyncState.Excluded;

            var node = new FolderTreeNode(Id: f.Id, Name: f.Name, ParentId: f.ParentId, AccountId: account.Id.Id, RemotePath: remotePath, SyncState: syncState, HasChildren: true);
            var vm = folderTreeNodeViewModelFactory.Create(node, tokenFactory, driveId.Value);

            vm.IncludeToggled += OnIncludeToggledAsync;
            vm.ViewActivityRequested += OnViewActivityRequested;
            vm.OpenInFileManagerRequested += OnOpenInFileManager;

            RootFolders.Add(vm);
        }
    }

    private async void OnIncludeToggledAsync(object? sender, FolderTreeNodeViewModel node)
    {
        try
        {
            var ruleType = node.IsIncluded ? RuleType.Include : RuleType.Exclude;

            var affected = ruleType == RuleType.Include
                ? CollectAllVisible([node])
                : [node];

            var ruleNodes = affected.Select(item => (item.RemotePath, item.Id)).ToList();
            int includedCount = await syncRuleService.ApplyRuleAsync(account.Id, node.RemotePath, ruleType, ruleNodes, CancellationToken.None);

            FolderCountChanged?.Invoke(this, includedCount);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.FolderSelectionPersistFailed(logger, account.Id.Id, ex.Message, ex);
        }
    }

    private void OnViewActivityRequested(object? sender, FolderTreeNodeViewModel node)
        => ViewActivityRequested?.Invoke(this, node);

    private void OnOpenInFileManager(object? sender, FolderTreeNodeViewModel node)
    {
        string oneDriveBase = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).CombinePath("OneDrive"));

        string candidatePath;
        try
        {
            candidatePath = Path.GetFullPath(oneDriveBase.CombinePath(node.Name));
        }
        catch (ArgumentException)
        {
            OneDriveSyncClientMessages.FileManagerPathEscapesBase(logger, node.Name);
            return;
        }

        if (!candidatePath.StartsWith(oneDriveBase + Path.DirectorySeparatorChar, StringComparison.Ordinal) && candidatePath != oneDriveBase)
        {
            OneDriveSyncClientMessages.FileManagerPathEscapesBase(logger, candidatePath);
            return;
        }

        if (!fileSystem.Directory.Exists(candidatePath))
            return;

        fileManagerService.OpenFolder(candidatePath);
    }

    private static IEnumerable<FolderTreeNodeViewModel> CollectAllVisible(IEnumerable<FolderTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;

            foreach (var descendant in CollectAllVisible(node.Children))
                yield return descendant;
        }
    }
}
