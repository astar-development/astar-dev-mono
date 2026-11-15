using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.FolderSelection;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>Creates <see cref="AccountDetailTabs"/> values.</summary>
public static class AccountDetailTabsFactory
{
    /// <summary>Creates the tabs' view models.</summary>
    /// <param name="conflicts">The account's conflicts.</param>
    /// <param name="folders">The account's folder selection.</param>
    /// <param name="activity">The account's activity.</param>
    public static AccountDetailTabs Create(ConflictListViewModel conflicts, FolderSelectionViewModel folders, ActivityListViewModel activity) => new(conflicts, folders, activity);
}
