using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

/// <summary>Creates a new <see cref="ConflictListViewModel"/> over the shared sources and services on every call.</summary>
/// <param name="sources">Supplies the accounts, their open items and their sync pairs.</param>
/// <param name="resolver">Resolves a conflict.</param>
/// <param name="heldDecider">Confirms or restores held deletions.</param>
/// <param name="planner">Says which strategies apply to a conflict.</param>
/// <param name="localization">Supplies the text.</param>
/// <param name="logger">Reports reads and resolutions that failed, without paths.</param>
public sealed class ConflictListFactory(ConflictListSources sources, IConflictResolver resolver, IHeldDeletionDecider heldDecider, IConflictResolutionPlanner planner, ILocalizationService localization, ILogger<ConflictListViewModel> logger) : IConflictListFactory
{
    /// <inheritdoc />
    public ConflictListViewModel Create(Option<AccountId> account) => new(sources, resolver, heldDecider, planner, localization, logger) { AccountFilter = account };
}
