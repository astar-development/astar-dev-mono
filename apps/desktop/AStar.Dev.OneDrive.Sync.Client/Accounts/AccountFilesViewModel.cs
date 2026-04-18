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
    private readonly OneDriveAccount    _account      = account;
    private readonly IAuthService       _authService  = authService;
    private readonly IGraphService      _graphService = graphService;
    private readonly IAccountRepository _repository   = repository;
    private string? _accessToken;
    private HashSet<OneDriveFolderId> _explicitExclusionIds = [];
    private HashSet<OneDriveFolderId> _allIncludedFolderIds = [];
    private HashSet<OneDriveFolderId> _selectedFolderIds    = [];

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
            var entity = await _repository.GetByIdAsync(_account.Id, CancellationToken.None);

            if(entity?.SyncFolders is { Count: > 0 })
                BuildFolderTreeFromDatabase(entity.SyncFolders);
            else
                await LoadFromOneDriveAsync();
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

    private async Task LoadFromOneDriveAsync()
    {
        var authResult = await _authService.AcquireTokenSilentAsync(_account.Id.Id);

        if(authResult.IsError)
        {
            LoadError    = authResult.ErrorMessage ?? "Authentication failed.";
            HasLoadError = true;
            return;
        }

        _accessToken          = authResult.AccessToken!;
        _explicitExclusionIds = _account.ExplicitlyExcludedFolderIds.ToHashSet();
        _allIncludedFolderIds = _account.AllIncludedFolderIds.ToHashSet();
        _selectedFolderIds    = _account.SelectedFolderIds.ToHashSet();

        var rootFolders = await _graphService.GetRootFoldersAsync(_accessToken);
        var allFolders  = await _graphService.GetAllFoldersAsync(_accessToken);

        var childrenByParentId = allFolders
            .Where(f => f.ParentId is not null)
            .GroupBy(f => f.ParentId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach(var folder in rootFolders)
        {
            var vm = BuildFolderNodeTree(folder, childrenByParentId, depth: 0, parentState: FolderSyncState.Excluded);
            vm.IncludeToggled             += OnIncludeToggled;
            vm.ViewActivityRequested      += OnViewActivityRequested;
            vm.OpenInFileManagerRequested += OnOpenInFileManager;
            RootFolders.Add(vm);
        }

        await PersistFolderDecisionsIfNeededAsync();
    }

    private void BuildFolderTreeFromDatabase(ICollection<SyncFolderEntity> syncFolders)
    {
        var roots = syncFolders
            .Where(f => !f.FolderName.Contains('/'))
            .OrderBy(f => f.FolderName);

        foreach(var rootEntity in roots)
        {
            var vm = BuildNodeFromEntity(rootEntity, syncFolders, depth: 0);
            vm.IncludeToggled             += OnIncludeToggled;
            vm.ViewActivityRequested      += OnViewActivityRequested;
            vm.OpenInFileManagerRequested += OnOpenInFileManager;
            RootFolders.Add(vm);
        }
    }

    private FolderTreeNodeViewModel BuildNodeFromEntity(SyncFolderEntity entity, ICollection<SyncFolderEntity> allFolders, int depth)
    {
        var syncState = entity.IsExplicitlyExcluded ? FolderSyncState.Excluded
                      : entity.IsIncluded           ? FolderSyncState.Included
                      : FolderSyncState.Excluded;

        var directChildren = allFolders
            .Where(f => IsDirectChild(f.FolderName, entity.FolderName))
            .OrderBy(f => f.FolderName)
            .ToList();

        var node = new FolderTreeNode(
            Id:          entity.FolderId.Id,
            Name:        LastPathSegment(entity.FolderName),
            ParentId:    null,
            AccountId:   _account.Id.Id,
            SyncState:   syncState,
            HasChildren: directChildren.Count > 0);

        var vm = new FolderTreeNodeViewModel(node, depth);

        foreach(var child in directChildren)
            vm.AddChild(BuildNodeFromEntity(child, allFolders, depth + 1));

        return vm;
    }

    private static bool IsDirectChild(string candidatePath, string parentPath)
    {
        if(!candidatePath.StartsWith(parentPath + "/", StringComparison.Ordinal))
            return false;

        return !candidatePath[(parentPath.Length + 1)..].Contains('/');
    }

    private static string LastPathSegment(string path)
        => path.Contains('/') ? path[(path.LastIndexOf('/') + 1)..] : path;

    private FolderTreeNodeViewModel BuildFolderNodeTree(DriveFolder folder, Dictionary<string, List<DriveFolder>> childrenByParentId, int depth, FolderSyncState parentState)
    {
        var children  = childrenByParentId.GetValueOrDefault(folder.Id) ?? [];
        var syncState = ResolveSyncState(new OneDriveFolderId(folder.Id), parentState);

        var node = new FolderTreeNode(
            Id:          folder.Id,
            Name:        folder.Name,
            ParentId:    folder.ParentId,
            AccountId:   _account.Id.Id,
            SyncState:   syncState,
            HasChildren: children.Count > 0);

        var vm = new FolderTreeNodeViewModel(node, depth);

        foreach(var child in children)
            vm.AddChild(BuildFolderNodeTree(child, childrenByParentId, depth + 1, syncState));

        return vm;
    }

    private FolderSyncState ResolveSyncState(OneDriveFolderId folderId, FolderSyncState parentState)
    {
        if(_explicitExclusionIds.Contains(folderId))
            return FolderSyncState.Excluded;

        if(_allIncludedFolderIds.Contains(folderId))
            return FolderSyncState.Included;

        if(_selectedFolderIds.Contains(folderId))
            return FolderSyncState.Included;

        return parentState is FolderSyncState.Included ? FolderSyncState.Included : FolderSyncState.Excluded;
    }

    private async Task PersistFolderDecisionsIfNeededAsync()
    {
        var decisions = CollectAllFolderDecisions(RootFolders, parentPath: string.Empty, parentIsIncluded: false).ToList();

        if(decisions.Count == 0)
            return;

        var entity = await _repository.GetByIdAsync(_account.Id, CancellationToken.None);
        if(entity is null)
            return;

        var incomingState = decisions.Select(d => (new OneDriveFolderId(d.Node.Id), d.IsIncluded, d.IsExplicitExclusion)).ToHashSet();
        var storedState   = entity.SyncFolders.Select(f => (f.FolderId, f.IsIncluded, f.IsExplicitlyExcluded)).ToHashSet();

        if(incomingState.SetEquals(storedState))
            return;

        entity.SyncFolders = BuildSyncFolderEntities(decisions);
        await _repository.UpsertAsync(entity, CancellationToken.None);
    }

    private async void OnIncludeToggled(object? sender, FolderTreeNodeViewModel node)
    {
        var entity = await _repository.GetByIdAsync(_account.Id, CancellationToken.None);
        if(entity is null)
            return;

        var decisions = CollectAllFolderDecisions(RootFolders, parentPath: string.Empty, parentIsIncluded: false).ToList();
        entity.SyncFolders = BuildSyncFolderEntities(decisions);

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

    private List<SyncFolderEntity> BuildSyncFolderEntities(List<(FolderTreeNodeViewModel Node, string RelativePath, bool IsIncluded, bool IsExplicitExclusion)> decisions)
        => [.. decisions.Select(d => new SyncFolderEntity
            {
                FolderId             = new OneDriveFolderId(d.Node.Id),
                FolderName           = d.RelativePath,
                AccountId            = _account.Id,
                IsIncluded           = d.IsIncluded,
                IsExplicitlyExcluded = d.IsExplicitExclusion
            })];

    private static IEnumerable<(FolderTreeNodeViewModel Node, string RelativePath, bool IsIncluded, bool IsExplicitExclusion)> CollectAllFolderDecisions(IEnumerable<FolderTreeNodeViewModel> nodes, string parentPath, bool parentIsIncluded)
    {
        foreach(var node in nodes)
        {
            var nodeRelativePath    = string.IsNullOrEmpty(parentPath) ? node.Name : $"{parentPath}/{node.Name}";
            var nodeIsIncluded      = node.SyncState is not FolderSyncState.Excluded;
            var isExplicitExclusion = !nodeIsIncluded && parentIsIncluded;

            yield return (node, nodeRelativePath, nodeIsIncluded, isExplicitExclusion);

            foreach(var descendant in CollectAllFolderDecisions(node.Children, nodeRelativePath, parentIsIncluded: nodeIsIncluded))
                yield return descendant;
        }
    }
}
