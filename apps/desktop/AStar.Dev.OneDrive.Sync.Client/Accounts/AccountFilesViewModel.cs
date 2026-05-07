using System.Collections.ObjectModel;
using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.Utilities;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FolderTreeNodeViewModel = AStar.Dev.OneDrive.Sync.Client.Home.FolderTreeNodeViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncRuleRepository syncRuleRepository, IFileSystem fileSystem) : ObservableObject
{
    private readonly OneDriveAccount _account = account;
    private readonly IAuthService _authService = authService;
    private readonly IGraphService _graphService = graphService;
    private readonly IAccountRepository _repository = repository;
    private readonly ISyncRuleRepository _syncRuleRepository = syncRuleRepository;
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
            var authResult = await _authService.AcquireTokenSilentAsync(_account.Id.Id);

            if(authResult is not Result<AuthResult, AuthError>.Ok ok)
            {
                LoadError = authResult is Result<AuthResult, AuthError>.Error { Reason: AuthFailedError failed }
                    ? failed.Message
                    : "Authentication failed.";
                HasLoadError = true;

                return;
            }

            _accessToken = ok.Value.AccessToken;
            var driveId = await _graphService.GetDriveIdAsync(_accessToken);
            _driveId = new Option<DriveId>.Some(driveId);

            var includedPaths = await LoadRulesAsync();
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

    private async Task<HashSet<string>> LoadRulesAsync()
    {
        var existingRules = await _syncRuleRepository.GetByAccountIdAsync(_account.Id, CancellationToken.None);

        return existingRules
            .Where(r => r.RuleType == RuleType.Include)
            .Select(r => r.RemotePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task BuildRootFoldersAsync(HashSet<string> includedPaths)
    {
        var folders = await _graphService.GetRootFoldersAsync(_accessToken!);

        foreach (var f in folders)
        {
            string remotePath = $"/{f.Name}";
            var syncState = includedPaths.Contains(remotePath)
                ? FolderSyncState.Included
                : FolderSyncState.Excluded;

            var node = new FolderTreeNode(Id: f.Id, Name: f.Name, ParentId: f.ParentId, AccountId: _account.Id.Id, RemotePath: remotePath, SyncState: syncState, HasChildren: true);

            var vm = _driveId.Match(
                id => new FolderTreeNodeViewModel(node, _graphService, _accessToken!, id),
                () => throw new InvalidOperationException("Drive ID not available."));

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
            try
            {
                var ruleType = node.IsIncluded ? RuleType.Include : RuleType.Exclude;

                Serilog.Log.Debug("[AccountFilesViewModel] Persisting {RuleType} rule for {Path} (account {AccountId})", ruleType, node.RemotePath, _account.Id.Id);

                await _syncRuleRepository.DeleteChildRulesAsync(_account.Id, node.RemotePath, CancellationToken.None);

                var affected = ruleType == RuleType.Include
                    ? CollectAllVisible([node])
                    : [node];

                foreach(var item in affected)
                    await _syncRuleRepository.UpsertAsync(_account.Id, item.RemotePath, ruleType, item.Id, CancellationToken.None);
            }
            catch(Exception ex)
            {
                Serilog.Log.Error(ex, "[AccountFilesViewModel] Failed to persist folder selection for account {AccountId} — {Error}", _account.Id.Id, ex.Message);
            }
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[AccountFilesViewModel.OnIncludeToggledAsync] Unhandled exception: {Error}", ex.Message);
        }
    }

    private void OnViewActivityRequested(object? sender, FolderTreeNodeViewModel node)
        => ViewActivityRequested?.Invoke(this, node);

    private void OnOpenInFileManager(object? sender, FolderTreeNodeViewModel node)
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).CombinePath("OneDrive", node.Name);

        if (!fileSystem.Directory.Exists(path))
            return;

        string opener = OperatingSystem.IsWindows() ? "explorer"
                   : OperatingSystem.IsMacOS() ? "open"
                   : "xdg-open";

        _ = System.Diagnostics.Process.Start(opener, path);
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
