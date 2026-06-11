using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

/// <summary>Container-backed factory for <see cref="ConflictItemViewModel"/> instances.</summary>
public sealed class ConflictItemViewModelFactory(ISyncService syncService, ILocalizationService localizationService) : IConflictItemViewModelFactory
{
    /// <inheritdoc />
    public ConflictItemViewModel Create(SyncConflict conflict) => new(conflict, syncService, localizationService);
}
