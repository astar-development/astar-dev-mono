using System.Collections.ObjectModel;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class FolderTreeNodeViewModel : ObservableObject
{
    private readonly IGraphService _graphService;
    private readonly string        _accessToken;
    private readonly DriveId       _driveId;
    private          bool          _childrenLoaded;

    public string Id { get; }
    public string Name { get; }
    public string? ParentId { get; }
    public string RemotePath { get; }
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

    public string ExpanderGlyph => IsExpanded ? "▾" : "▸";

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public event EventHandler<FolderTreeNodeViewModel>? IncludeToggled;
    public event EventHandler<FolderTreeNodeViewModel>? OpenInFileManagerRequested;
    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    public FolderTreeNodeViewModel(FolderTreeNode node, IGraphService graphService, string accessToken, DriveId driveId, int depth = 0)
    {
        Id = node.Id;
        Name = node.Name;
        ParentId = node.ParentId;
        RemotePath = node.RemotePath;
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
        var newState = SyncState is FolderSyncState.Excluded
            ? FolderSyncState.Included
            : FolderSyncState.Excluded;

        ApplySyncStateRecursively(newState);
        IncludeToggled?.Invoke(this, this);
    }

    internal void ApplySyncStateRecursively(FolderSyncState state)
    {
        SyncState = state;

        foreach(var child in Children)
            child.ApplySyncStateRecursively(state);
    }

    [RelayCommand]
    private void OpenInFileManager()
        => OpenInFileManagerRequested?.Invoke(this, this);

    [RelayCommand]
    private void ViewActivity()
        => ViewActivityRequested?.Invoke(this, this);

    private async Task EnsureChildrenLoadedAsync()
    {
        if(_childrenLoaded) return;

        IsLoadingChildren = true;
        try
        {
            var folders = await _graphService.GetChildFoldersAsync(_accessToken, _driveId, Id)
                .MatchAsync<List<DriveFolder>, string, List<DriveFolder>?>(
                    f => f,
                    error =>
                    {
                        Serilog.Log.Warning("[FolderTreeNodeViewModel] Failed to load children for {Path}: {Error}", RemotePath, error);
                        HasChildren = false;
                        return null;
                    });

            if(folders is null)
                return;

            Children.Clear();
            foreach(var f in folders)
            {
                var childVm = CreateChildFolderTreeViewModel(f);

                Children.Add(childVm);
            }

            if(Children.Count == 0) HasChildren = false;

            _childrenLoaded = true;
        }
        finally
        {
            IsLoadingChildren = false;
        }
    }

    private FolderTreeNodeViewModel CreateChildFolderTreeViewModel(DriveFolder f)
    {
        string childRemotePath = $"{RemotePath}/{f.Name}";
        var childNode = MapDriveFolderToChildNode(f, childRemotePath);

        return CreateChildFolderTreeViewModel(childNode);
    }

    private FolderTreeNodeViewModel CreateChildFolderTreeViewModel(FolderTreeNode childNode)
    {
        var childVm = new FolderTreeNodeViewModel(childNode, _graphService, _accessToken, _driveId, Depth + 1);

        childVm.IncludeToggled += (s, e) => IncludeToggled?.Invoke(s, e);
        childVm.OpenInFileManagerRequested += (s, e) => OpenInFileManagerRequested?.Invoke(s, e);
        childVm.ViewActivityRequested += (s, e) => ViewActivityRequested?.Invoke(s, e);

        return childVm;
    }

    private FolderTreeNode MapDriveFolderToChildNode(DriveFolder f, string childRemotePath)
        => new(f.Id, f.Name, f.ParentId, AccountId: string.Empty, RemotePath: childRemotePath, SyncState, HasChildren: true);
}
