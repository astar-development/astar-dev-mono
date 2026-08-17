using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Localization;

namespace AStarDev.OneDriveSyncClient.Conflicts;

/// <summary>Container-backed factory for <see cref="ConflictItemViewModel"/> instances.</summary>
public sealed class ConflictItemViewModelFactory(ISyncService syncService, ILocalizationService localizationService) : IConflictItemViewModelFactory
{
    /// <inheritdoc />
    public ConflictItemViewModel Create(SyncConflict conflict) => new(conflict, syncService, localizationService);
}
