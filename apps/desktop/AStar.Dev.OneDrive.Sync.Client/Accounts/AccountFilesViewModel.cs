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

public sealed partial class AccountFilesViewModel(OneDriveAccount account, IAuthService authService, IGraphService graphService, IAccountRepository repository) : ObservableObject
{
    private readonly OneDriveAccount    _account     = account;
    private readonly IAuthService       _authService = authService;
    private readonly IGraphService      _graphService = graphService;
    private readonly IAccountRepository _repository  = repository;
    private string? _accessToken;
    private HashSet<OneDriveFolderId> _explicitExclusionIds = [];

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
        if(IsLoading)
            return;

        IsLoading    = true;
        HasLoadError = false;
        LoadError    = string.Empty;
        RootFolders.Clear();

        try
        {
            var authResult = await _authService.AcquireTokenSilentAsync(_account.Id.Id);

            if(authResult.IsError)
            {
                LoadError    = authResult.ErrorMessage ?? "Authentication failed.";
                HasLoadError = true;
                return;
            }

            _accessToken = authResult.AccessToken!;
            _explicitExclusionIds = _account.ExplicitlyExcludedFolderIds.ToHashSet();

            var rootFolders = await _graphService.GetRootFoldersAsync(_accessToken);
            var allFolders  = await _graphService.GetAllFoldersAsync(_accessToken);

            var childrenByParentId = allFolders
                .Where(f => f.ParentId is not null)
                .GroupBy(f => f.ParentId!)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach(var folder in rootFolders)
            {
                var vm = BuildFolderNodeTree(folder, childrenByParentId, depth: 0);
                vm.IncludeToggled            += OnIncludeToggled;
                vm.ViewActivityRequested     += OnViewActivityRequested;
                vm.OpenInFileManagerRequested += OnOpenInFileManager;
                RootFolders.Add(vm);
            }
        }
        catch(Exception ex)
        {
            LoadError    = $"Failed to load folders: {ex.Message}";
            HasLoadError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private FolderTreeNodeViewModel BuildFolderNodeTree(DriveFolder folder, Dictionary<string, List<DriveFolder>> childrenByParentId, int depth)
    {
        var children = childrenByParentId.GetValueOrDefault(folder.Id) ?? [];

        var node = new FolderTreeNode(
            Id:          folder.Id,
            Name:        folder.Name,
            ParentId:    folder.ParentId,
            AccountId:   _account.Id.Id,
            SyncState:   ResolveSyncState(new OneDriveFolderId(folder.Id)),
            HasChildren: children.Count > 0);

        var vm = new FolderTreeNodeViewModel(node, depth);

        foreach(var child in children)
            vm.AddChild(BuildFolderNodeTree(child, childrenByParentId, depth + 1));

        return vm;
    }

    private FolderSyncState ResolveSyncState(OneDriveFolderId folderId)
    {
        if(_explicitExclusionIds.Contains(folderId))
            return FolderSyncState.Excluded;

        return _account.SelectedFolderIds.Contains(folderId)
            ? FolderSyncState.Included
            : FolderSyncState.Excluded;
    }

    private async void OnIncludeToggled(object? sender, FolderTreeNodeViewModel node)
    {
        var entity = await _repository.GetByIdAsync(_account.Id, CancellationToken.None);
        if(entity is null)
            return;

        entity.SyncFolders = [.. CollectSyncDecisions(RootFolders, parentPath: string.Empty, parentIsIncluded: false)
            .Select(d => new SyncFolderEntity
            {
                FolderId             = new OneDriveFolderId(d.Node.Id),
                FolderName           = d.RelativePath,
                AccountId            = _account.Id,
                IsExplicitlyExcluded = d.IsExplicitExclusion
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

        if(!Directory.Exists(path))
            return;

        string opener = OperatingSystem.IsWindows() ? "explorer"
                   : OperatingSystem.IsMacOS()      ? "open"
                   : "xdg-open";

        _ = System.Diagnostics.Process.Start(opener, path);
    }

    /// <summary>
    /// Builds the minimal set of folder decisions to persist.
    /// Only root-level included folders and explicitly-excluded sub-folders are stored.
    /// </summary>
    private static IEnumerable<(FolderTreeNodeViewModel Node, string RelativePath, bool IsExplicitExclusion)> CollectSyncDecisions(IEnumerable<FolderTreeNodeViewModel> nodes, string parentPath, bool parentIsIncluded)
    {
        foreach(var node in nodes)
        {
            var nodeRelativePath = string.IsNullOrEmpty(parentPath) ? node.Name : $"{parentPath}/{node.Name}";
            var nodeIsIncluded   = node.SyncState is not FolderSyncState.Excluded;

            if(nodeIsIncluded && !parentIsIncluded)
            {
                yield return (node, nodeRelativePath, false);
                foreach(var descendant in CollectSyncDecisions(node.Children, nodeRelativePath, parentIsIncluded: true))
                    yield return descendant;
            }
            else if(!nodeIsIncluded && parentIsIncluded)
            {
                yield return (node, nodeRelativePath, true);
            }
            else if(nodeIsIncluded && parentIsIncluded)
            {
                foreach(var descendant in CollectSyncDecisions(node.Children, nodeRelativePath, parentIsIncluded: true))
                    yield return descendant;
            }
        }
    }
}
