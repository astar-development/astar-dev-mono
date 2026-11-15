using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Activity;

/// <summary>Creates activity lists for hosting inside a page, independent of each other.</summary>
public interface IActivityListFactory
{
    /// <summary>Creates a list that shows nothing until its refresh command runs.</summary>
    /// <param name="overview">Supplies the accounts the entries belong to.</param>
    /// <param name="account">The only account whose activity the list shows, or none for every account.</param>
    /// <returns>The new list.</returns>
    ActivityListViewModel Create(SyncOverviewViewModel overview, Option<AccountId> account);
}
