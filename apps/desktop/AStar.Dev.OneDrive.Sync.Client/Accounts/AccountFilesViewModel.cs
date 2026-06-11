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

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncRuleService syncRuleService, IFileSystem fileSystem, IFileManagerService fileManagerService, ILogger<AccountFilesViewModel> logger, ILogger<FolderTreeNodeViewModel> folderTreeLogger, ILocalizationService localizationService) : ObservableObject
{
    private readonly OneDriveAccount _account = account;
    private readonly IAuthService _authService = authService;
    private readonly IGraphService _graphService = graphService;
    private readonly IAccountRepository _repository = repository;
    private readonly ISyncRuleService _syncRuleService = syncRuleService;
    private readonly IFileManagerService _fileManagerService = fileManagerService;
    private readonly ILogger<AccountFilesViewModel> _logger = logger;
    private readonly ILogger<FolderTreeNodeViewModel> _folderTreeLogger = folderTreeLogger;
    private readonly ILocalizationService _localizationService = localizationService;
    private string? _accessToken;
    private Option<DriveId> _driveId = DriveIdFactory.Empty;

    /// <summary>The unique identifier for the account.</summary>
    public string AccountId => _account.Id.Id;
    public string DisplayName => _account.Profile.DisplayName;
    public string Email => _account.Profile.Email;

    public string TabLabel => _account.Profile.DisplayName.Length > 0
                                 ? _account.Profile.DisplayName
                                 : _account.Profile.Email;

    public int AccentIndex => _account.AccentIndex;

    public Color AccentColor => AccountCardViewModel.PaletteColor(_account.AccentIndex);

    public ObservableCollection<FolderTreeNodeViewModel> RootFolders { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string LoadError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial bool IsActiveTab { get; set; }

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
            _accessToken = await _authService.AcquireTokenSilentAsync(_account.Id.Id)
                .MatchAsync<AuthResult, AuthError, string?>(
                    ok => ok.AccessToken,
                    error =>
                    {
                        LoadError = error is AuthFailedError failed ? failed.Message : "Authentication failed.";
                        HasLoadError = true;
                        return null;
                    });

            if (_accessToken is null)
                return;

            var driveId = await _graphService.GetDriveIdAsync(_account.Id.Id, _ => Task.FromResult(_accessToken ?? string.Empty))
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

            _driveId = new Option<DriveId>.Some(driveId.Value);

            var includedPaths = await _syncRuleService.GetIncludedPathsAsync(_account.Id, CancellationToken.None);
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
        var folders = await _graphService.GetRootFoldersAsync(_account.Id.Id, _ => Task.FromResult(_accessToken ?? string.Empty))
            .MatchAsync<List<DriveFolder>, string, List<DriveFolder>?>(
                f => f,
                error =>
                {
                    OneDriveSyncClientMessages.RootFoldersLoadFailed(_logger, _account.Id.Id, error);
                    LoadError = error;
                    HasLoadError = true;
                    return null;
                });

        if (folders is null)
            return;

        var driveId = _driveId.Match<DriveId?>(
            id => id,
            () =>
            {
                OneDriveSyncClientMessages.DriveIdNotAvailable(_logger, _account.Id.Id);
                return null;
            });

        if (driveId is null)
            return;

        Func<CancellationToken, Task<string>> tokenFactory = _ => Task.FromResult(_accessToken ?? string.Empty);

        foreach (var f in folders)
        {
            string remotePath = $"/{f.Name}";
            var syncState = includedPaths.Contains(remotePath)
                ? FolderSyncState.Included
                : FolderSyncState.Excluded;

            var node = new FolderTreeNode(Id: f.Id, Name: f.Name, ParentId: f.ParentId, AccountId: _account.Id.Id, RemotePath: remotePath, SyncState: syncState, HasChildren: true);
            var vm = new FolderTreeNodeViewModel(node, _graphService, tokenFactory, driveId.Value, _folderTreeLogger, _localizationService);

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
            int includedCount = await _syncRuleService.ApplyRuleAsync(_account.Id, node.RemotePath, ruleType, ruleNodes, CancellationToken.None);

            FolderCountChanged?.Invoke(this, includedCount);
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.FolderSelectionPersistFailed(_logger, _account.Id.Id, ex.Message, ex);
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
            OneDriveSyncClientMessages.FileManagerPathEscapesBase(_logger, node.Name);
            return;
        }

        if (!candidatePath.StartsWith(oneDriveBase + Path.DirectorySeparatorChar, StringComparison.Ordinal) && candidatePath != oneDriveBase)
        {
            OneDriveSyncClientMessages.FileManagerPathEscapesBase(_logger, candidatePath);
            return;
        }

        if (!fileSystem.Directory.Exists(candidatePath))
            return;

        _fileManagerService.OpenFolder(candidatePath);
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
