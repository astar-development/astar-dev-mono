using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.ViewModels;

public sealed partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IGraphService _graphService;
    private readonly string        _accessToken;
    private readonly string        _driveId;
    private          bool          _childrenLoaded;

    public string Id { get; }
    public string Name { get; }
    public string? ParentId { get; }
    public int Depth { get; }

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
        FolderSyncState.Synced => "synced",
        FolderSyncState.Syncing => "syncing ...",
        FolderSyncState.Partial => "partial",
        FolderSyncState.Conflict => "conflict",
        FolderSyncState.Error => "error",
        _ => "excluded"
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
    public event EventHandler<FolderTreeNodeViewModel>? OpenInFileManagerRequested;
    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    public FolderTreeNodeViewModel(
        FolderTreeNode node,
        IGraphService graphService,
        string accessToken,
        string driveId,
        int depth = 0)
    {
        Id = node.Id;
        Name = node.Name;
        ParentId = node.ParentId;
        Depth = depth;
        SyncState = node.SyncState;
        HasChildren = node.HasChildren;
        _graphService = graphService;
        _accessToken = accessToken;
        _driveId = driveId;
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
    private void ToggleInclude()
    {
        SyncState = SyncState is FolderSyncState.Excluded
            ? FolderSyncState.Included
            : FolderSyncState.Excluded;

        IncludeToggled?.Invoke(this, this);
    }

    [RelayCommand]
    private void OpenInFileManager()
        => OpenInFileManagerRequested?.Invoke(this, this);

    [RelayCommand]
    private void ViewActivity()
        => ViewActivityRequested?.Invoke(this, this);

    private async Task EnsureChildrenLoadedAsync()
    {
        if(_childrenLoaded)
            return;

        IsLoadingChildren = true;
        try
        {
            var folders = await _graphService
                .GetChildFoldersAsync(_accessToken, _driveId, Id);

            Children.Clear();
            foreach(var f in folders)
            {
                var childNode = new FolderTreeNode(
                    Id:          f.Id,
                    Name:        f.Name,
                    ParentId:    f.ParentId,
                    AccountId:   string.Empty,
                    SyncState:   FolderSyncState.Excluded,
                    HasChildren: true);

                var childVm = new FolderTreeNodeViewModel(
                    childNode, _graphService, _accessToken, _driveId, Depth + 1);

                childVm.IncludeToggled += (s, e) => IncludeToggled?.Invoke(s, e);
                childVm.OpenInFileManagerRequested += (s, e) => OpenInFileManagerRequested?.Invoke(s, e);
                childVm.ViewActivityRequested += (s, e) => ViewActivityRequested?.Invoke(s, e);

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
