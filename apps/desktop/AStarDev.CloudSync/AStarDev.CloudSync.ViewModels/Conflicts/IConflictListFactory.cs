using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>Creates conflict lists for hosting inside a page, each independent of the modal list and of each other.</summary>
public interface IConflictListFactory
{
    /// <summary>Creates a list that is not open as a dialog and has read nothing yet.</summary>
    /// <param name="account">The only account whose conflicts the list shows, or none for every account.</param>
    /// <returns>The new list; call its refresh command to read the conflicts.</returns>
    ConflictListViewModel Create(Option<AccountId> account);
}
