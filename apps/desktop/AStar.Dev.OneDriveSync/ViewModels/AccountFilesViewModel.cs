using System.Collections.ObjectModel;
using Avalonia.Media;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class AccountFilesViewModel : ReactiveObject
{
    public string AccountId { get; init; } = string.Empty;
    public string TabLabel { get; init; } = string.Empty;
    public Color AccentColor { get; init; }
    public ObservableCollection<FolderTreeNodeViewModel> RootFolders { get; } = [];

    public bool IsActiveTab
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool HasLoadError
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string LoadError
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

#pragma warning disable CA1822 // Mark members as static
    public Task ActivateAsync() => Task.CompletedTask;
#pragma warning restore CA1822 // Mark members as static
}
