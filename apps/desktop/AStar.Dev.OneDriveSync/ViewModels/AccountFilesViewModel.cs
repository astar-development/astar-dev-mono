using System.Collections.ObjectModel;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountFilesViewModel : ReactiveObject
{
    private bool _isActiveTab;
    private bool _isLoading;
    private bool _hasLoadError;
    private string _loadError = string.Empty;

    public string AccountId { get; init; } = string.Empty;
    public string TabLabel { get; init; } = string.Empty;
    public Color AccentColor { get; init; }
    public ObservableCollection<FolderTreeNodeViewModel> RootFolders { get; } = [];

    public bool IsActiveTab
    {
        get => _isActiveTab;
        set => this.RaiseAndSetIfChanged(ref _isActiveTab, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool HasLoadError
    {
        get => _hasLoadError;
        set => this.RaiseAndSetIfChanged(ref _hasLoadError, value);
    }

    public string LoadError
    {
        get => _loadError;
        set => this.RaiseAndSetIfChanged(ref _loadError, value);
    }

    public Task ActivateAsync() => Task.CompletedTask;
}
