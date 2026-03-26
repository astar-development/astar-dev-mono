using System.Collections.ObjectModel;
using System.Windows.Input;
using AStar.Dev.OneDriveSync.Models;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class FolderTreeNodeViewModel : ReactiveObject
{
    public string FolderId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Depth { get; init; }
    public SyncState SyncState { get; init; }
    public string StatusBadgeText { get; init; } = string.Empty;
    public bool HasChildren { get; init; }
    public ObservableCollection<FolderTreeNodeViewModel> Children { get; } = [];

    public bool IsExpanded
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsIncluded
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool IsLoadingChildren
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ExpanderGlyph => IsExpanded ? "▲" : "▶";

    public ICommand ToggleExpandCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ToggleIncludeCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand OpenInFileManagerCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ViewActivityCommand { get; init; } = ReactiveCommand.Create(() => { });
}
