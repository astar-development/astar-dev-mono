using ReactiveUI;

namespace AStar.Dev.OneDriveSync.old.ViewModels;

public class WizardFolderItem : ReactiveObject
{
    public string FolderId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}
