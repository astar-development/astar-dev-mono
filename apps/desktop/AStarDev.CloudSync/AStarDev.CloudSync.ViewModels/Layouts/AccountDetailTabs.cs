using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.FolderSelection;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>The view models behind the tabs of an account's detail that show more than the overview.</summary>
public sealed record AccountDetailTabs
{
    internal AccountDetailTabs(ConflictListViewModel conflicts, FolderSelectionViewModel folders)
    {
        Conflicts = conflicts;
        Folders = folders;
    }

    /// <summary>The account's conflicts.</summary>
    public ConflictListViewModel Conflicts { get; }

    /// <summary>The account's folder selection.</summary>
    public FolderSelectionViewModel Folders { get; }
}
