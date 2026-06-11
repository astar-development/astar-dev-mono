using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Conflicts;

/// <summary>Creates <see cref="ConflictItemViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IConflictItemViewModelFactory
{
    /// <summary>Creates a conflict item view model for the supplied conflict.</summary>
    ConflictItemViewModel Create(SyncConflict conflict);
}
