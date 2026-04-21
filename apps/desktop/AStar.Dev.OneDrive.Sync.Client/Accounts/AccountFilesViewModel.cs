using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FolderTreeNodeViewModel = AStar.Dev.OneDrive.Sync.Client.Home.FolderTreeNodeViewModel;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository, ISyncRuleRepository syncRuleRepository) : ObservableObject
{
    private readonly OneDriveAccount _account = account;
    private readonly IAuthService _authService = authService;
    private readonly IGraphService _graphService = graphService;
    private readonly IAccountRepository _repository = repository;
    private readonly ISyncRuleRepository _syncRuleRepository = syncRuleRepository;
    private string? _accessToken;
    private string? _driveId;

    /// <summary>The unique identifier for the account.</summary>
    public string AccountId => _account.Id.Id;
    public string DisplayName => _account.DisplayName;
    public string Email => _account.Email;

    public string TabLabel => _account.DisplayName.Length > 0
                                 ? _account.DisplayName
                                 : _account.Email;

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

            if (authResult.IsError)
            {
                LoadError = authResult.ErrorMessage ?? "Authentication failed.";
                HasLoadError = true;
                return;
            }

            _accessToken = authResult.AccessToken!;
            _driveId = await _graphService.GetDriveIdAsync(_accessToken);

            var existingRules = await _syncRuleRepository.GetByAccountIdAsync(_account.Id, CancellationToken.None);
            var includedPaths = existingRules
                .Where(r => r.RuleType == RuleType.Include)
                .Select(r => r.RemotePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var folders = await _graphService.GetRootFoldersAsync(_accessToken);

            foreach (var f in folders)
            {
                string remotePath = $"/{f.Name}";
                var syncState = includedPaths.Contains(remotePath)
                    ? FolderSyncState.Included
                    : FolderSyncState.Excluded;

                var node = new FolderTreeNode(
                    Id: f.Id,
                    Name: f.Name,
                    ParentId: f.ParentId,
                    AccountId: _account.Id.Id,
                    RemotePath: remotePath,
                    SyncState: syncState,
                    HasChildren: true);

                var vm = new FolderTreeNodeViewModel(
                    node, _graphService, _accessToken, _driveId);

                vm.IncludeToggled += OnIncludeToggled;
                vm.ViewActivityRequested += OnViewActivityRequested;
                vm.OpenInFileManagerRequested += OnOpenInFileManager;

                RootFolders.Add(vm);
            }
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

    private async void OnIncludeToggled(object? sender, FolderTreeNodeViewModel node)
    {
        var ruleType = node.IsIncluded ? RuleType.Include : RuleType.Exclude;

        await _syncRuleRepository.UpsertAsync(_account.Id, node.RemotePath, ruleType, CancellationToken.None);

        var entity = await _repository.GetByIdAsync(_account.Id, CancellationToken.None);
        if (entity is null)
            return;

        entity.SyncFolders = [.. CollectAllVisible(RootFolders)
            .Where(f => f.IsIncluded)
            .Select(f => new SyncFolderEntity
            {
                FolderId   = new OneDriveFolderId(f.Id),
                FolderName = f.Name,
                AccountId  = _account.Id
            })];

        await _repository.UpsertAsync(entity, CancellationToken.None);
    }

    private void OnViewActivityRequested(object? sender, FolderTreeNodeViewModel node)
        => ViewActivityRequested?.Invoke(this, node);

    private static void OnOpenInFileManager(object? sender, FolderTreeNodeViewModel node)
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", node.Name);

        if (!Directory.Exists(path))
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
