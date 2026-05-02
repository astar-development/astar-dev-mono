using CommunityToolkit.Mvvm.ComponentModel;

namespace AStar.Dev.OneDrive.Sync.Client.Onboarding;

public sealed partial class WizardFolderItem(string id, string name) : ObservableObject
{
    public string Id { get; } = id;
    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;
}
