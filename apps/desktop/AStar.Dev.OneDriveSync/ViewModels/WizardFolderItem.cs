using ReactiveUI;

namespace AStar.Dev.OneDriveSync.ViewModels;

public class WizardFolderItem : ReactiveObject
{
    private bool _isSelected;

    public string FolderId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
}
