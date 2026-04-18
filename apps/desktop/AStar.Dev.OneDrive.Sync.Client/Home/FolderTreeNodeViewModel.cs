using System.Collections.ObjectModel;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStar.Dev.OneDrive.Sync.Client.Home;

public sealed partial class FolderTreeNodeViewModel : ObservableObject
{
    public string Id { get; }
    public string Name { get; }
    public string? ParentId { get; }
    public int Depth { get; }

    /// <summary>
    /// The state new children should inherit.
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
    public partial bool HasChildren { get; set; }

    public string ExpanderGlyph => IsExpanded ? "\u25BE" : "\u25B8";

    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public event EventHandler<FolderTreeNodeViewModel>? IncludeToggled;
    public event EventHandler<FolderTreeNodeViewModel>? ChildStateChanged;
    public event EventHandler<FolderTreeNodeViewModel>? OpenInFileManagerRequested;
    public event EventHandler<FolderTreeNodeViewModel>? ViewActivityRequested;

    public FolderTreeNodeViewModel(FolderTreeNode node, int depth = 0)
    {
        Id             = node.Id;
        Name           = node.Name;
        ParentId       = node.ParentId;
        Depth          = depth;
        SyncState      = node.SyncState;
        InheritedState = node.SyncState;
        HasChildren    = node.HasChildren;
    }

    /// <summary>
    /// Adds a pre-built child node and wires all event propagation.
    /// Called at tree-build time in <see cref="AccountFilesViewModel"/> — never after initial load.
    /// </summary>
    public void AddChild(FolderTreeNodeViewModel child)
    {
        child.ChildStateChanged          += (_, _) => RecalculateStateFromChildren();
        child.IncludeToggled             += (sender, node) => IncludeToggled?.Invoke(sender, node);
        child.OpenInFileManagerRequested += (sender, node) => OpenInFileManagerRequested?.Invoke(sender, node);
        child.ViewActivityRequested      += (sender, node) => ViewActivityRequested?.Invoke(sender, node);
        Children.Add(child);
    }

    [RelayCommand]
    private void ToggleExpand()
    {
        if(!HasChildren)
            return;

        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private void ToggleInclude()
    {
        SyncState      = SyncState is FolderSyncState.Excluded ? FolderSyncState.Included : FolderSyncState.Excluded;
        InheritedState = SyncState;

        if(SyncState is FolderSyncState.Included)
            ApplyIncludedToAllDescendants();
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

    private void ApplyIncludedToAllDescendants()
    {
        foreach(var child in Children)
        {
            child.SyncState      = FolderSyncState.Included;
            child.InheritedState = FolderSyncState.Included;
            child.ApplyIncludedToAllDescendants();
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
}
