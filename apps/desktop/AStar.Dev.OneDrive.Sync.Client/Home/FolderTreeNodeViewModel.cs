using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IGraphService                  _graphService;
    private readonly string                         _accessToken;
    private readonly string                         _driveId;
    private readonly IReadOnlySet<OneDriveFolderId> _explicitExclusions;
    private          bool                           _childrenLoaded;

    public string Id { get; }
    public string Name { get; }
    public string? ParentId { get; }
    public int Depth { get; }

    /// <summary>
    /// The state new lazily-loaded children should inherit.
    /// Reflects the most recent explicit user action on this node (not the computed Partial state).
    /// </summary>
    public FolderSyncState InheritedState { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIncluded))]
    [NotifyPropertyChangedFor(nameof(IsExcluded))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeText))]
    public partial FolderSyncState SyncState { get; set; }

    public bool IsIncluded => SyncState is not FolderSyncState.Excluded;
    public bool IsExcluded => SyncState is FolderSyncState.Excluded;

    public string StatusBadgeText => SyncState switch
    {
        FolderSyncState.Included => "included",
        FolderSyncState.Synced   => "synced",
        FolderSyncState.Syncing  => "syncing ...",
        FolderSyncState.Partial  => "partial",
        FolderSyncState.Conflict => "conflict",
        FolderSyncState.Error    => "error",
        _                        => "excluded"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderGlyph))]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingChildren { get; set; }

    [ObservableProperty]
    public partial bool HasChildren { get; set; }

    public string ExpanderGlyph => IsExpanded ? "\u25BE" : "\u25B8";

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public event EventHandler<FolderTreeNodeViewModel>? IncludeToggled;
    public event EventHandler<FolderTreeNodeViewModel>? ChildStateChanged;
    public event EventHandler<FolderTreeNodeViewModel>? OpenInFileManagerRequested;
    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    public FolderTreeNodeViewModel(FolderTreeNode node, IGraphService graphService, string accessToken, string driveId, IReadOnlySet<OneDriveFolderId> explicitExclusions, int depth = 0)
    {
        Id                  = node.Id;
        Name                = node.Name;
        ParentId            = node.ParentId;
        Depth               = depth;
        SyncState           = node.SyncState;
        InheritedState      = node.SyncState;
        HasChildren         = node.HasChildren;
        _graphService       = graphService;
        _accessToken        = accessToken;
        _driveId            = driveId;
        _explicitExclusions = explicitExclusions;
    }

    [RelayCommand]
    private async Task ToggleExpandAsync()
    {
        if(!HasChildren)
            return;

        if(!IsExpanded)
        {
            await EnsureChildrenLoadedAsync();
            IsExpanded = true;
        }
        else
        {
            IsExpanded = false;
        }
    }

    [RelayCommand]
    private async Task ToggleIncludeAsync()
    {
        SyncState      = SyncState is FolderSyncState.Excluded ? FolderSyncState.Included : FolderSyncState.Excluded;
        InheritedState = SyncState;

        if(SyncState is FolderSyncState.Included)
            await DeepLoadAndIncludeAsync();
        else
            CascadeStateToDescendants(FolderSyncState.Excluded);

        ChildStateChanged?.Invoke(this, this);
        IncludeToggled?.Invoke(this, this);
    }

    [RelayCommand]
    private void OpenInFileManager()
        => OpenInFileManagerRequested?.Invoke(this, this);

    [RelayCommand]
    private void ViewActivity()
        => ViewActivityRequested?.Invoke(this, this);

    private async Task DeepLoadAndIncludeAsync()
    {
        await EnsureChildrenLoadedAsync();
        foreach(var child in Children)
        {
            child.SyncState      = FolderSyncState.Included;
            child.InheritedState = FolderSyncState.Included;
            await child.DeepLoadAndIncludeAsync();
        }
    }

    private void CascadeStateToDescendants(FolderSyncState state)
    {
        foreach(var child in Children)
        {
            child.SyncState      = state;
            child.InheritedState = state;
            child.CascadeStateToDescendants(state);
        }
    }

    private void RecalculateStateFromChildren()
    {
        if(Children.Count == 0)
            return;

        var hasPartial  = Children.Any(c => c.SyncState is FolderSyncState.Partial);
        var hasIncluded = Children.Any(c => c.SyncState is not FolderSyncState.Excluded and not FolderSyncState.Partial);
        var hasExcluded = Children.Any(c => c.SyncState is FolderSyncState.Excluded);

        var newState = hasPartial || (hasIncluded && hasExcluded)
            ? FolderSyncState.Partial
            : hasIncluded
                ? InheritedState is FolderSyncState.Excluded ? FolderSyncState.Partial : FolderSyncState.Included
                : hasExcluded
                    ? InheritedState is FolderSyncState.Included ? FolderSyncState.Partial : FolderSyncState.Excluded
                    : SyncState;

        if(SyncState == newState)
            return;

        SyncState = newState;
        ChildStateChanged?.Invoke(this, this);
    }

    private async Task EnsureChildrenLoadedAsync()
    {
        if(_childrenLoaded)
            return;

        IsLoadingChildren = true;
        try
        {
            var folders = await _graphService.GetChildFoldersAsync(_accessToken, _driveId, Id) ?? [];

            Children.Clear();
            foreach(var f in folders)
            {
                var folderId       = new OneDriveFolderId(f.Id);
                var childSyncState = _explicitExclusions.Contains(folderId) ? FolderSyncState.Excluded : InheritedState;

                var childNode = new FolderTreeNode(
                    Id:          f.Id,
                    Name:        f.Name,
                    ParentId:    f.ParentId,
                    AccountId:   string.Empty,
                    SyncState:   childSyncState,
                    HasChildren: true);

                var childVm = new FolderTreeNodeViewModel(
                    childNode, _graphService, _accessToken, _driveId, _explicitExclusions, Depth + 1);

                childVm.ChildStateChanged         += (_, _) => RecalculateStateFromChildren();
                childVm.IncludeToggled            += (s, e) => IncludeToggled?.Invoke(s, e);
                childVm.OpenInFileManagerRequested += (s, e) => OpenInFileManagerRequested?.Invoke(s, e);
                childVm.ViewActivityRequested     += (s, e) => ViewActivityRequested?.Invoke(s, e);

                Children.Add(childVm);
            }

            if(Children.Count == 0)
                HasChildren = false;

            _childrenLoaded = true;
        }
        finally
        {
            IsLoadingChildren = false;
        }
    }
}
