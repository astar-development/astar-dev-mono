using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class LogViewerViewModel : ReactiveObject
{
    private string _searchText = string.Empty;

    public ObservableCollection<LogEntryViewModel> FilteredEntries { get; } = [];
    public bool HasEntries => FilteredEntries.Count > 0;
    public string EntryCountText => $"{FilteredEntries.Count} entries";

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public ICommand CloseCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ShowAllCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ShowErrorsCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ShowWarningsCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ShowDebugCommand { get; init; } = ReactiveCommand.Create(() => { });
    public ICommand ClearCommand { get; init; } = ReactiveCommand.Create(() => { });
}
