using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class FolderTreeNodeViewModel : ReactiveObject
{
    private bool _isExpanded;
    private bool _isIncluded = true;
    private bool _isLoadingChildren;

    public string FolderId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Depth { get; init; }
    public SyncState SyncState { get; init; }
    public string StatusBadgeText { get; init; } = string.Empty;
    public bool HasChildren { get; init; }
    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    public bool IsIncluded
    {
        get => _isIncluded;
        set => this.RaiseAndSetIfChanged(ref _isIncluded, value);
    }

    public bool IsLoadingChildren
    {
        get => _isLoadingChildren;
        set => this.RaiseAndSetIfChanged(ref _isLoadingChildren, value);
    }

    public string ExpanderGlyph => IsExpanded ? "▲" : "▶";

    public ICommand ToggleExpandCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ToggleIncludeCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand OpenInFileManagerCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ViewActivityCommand { get; init; } = ReactiveCommand.Create(() => { });
}
