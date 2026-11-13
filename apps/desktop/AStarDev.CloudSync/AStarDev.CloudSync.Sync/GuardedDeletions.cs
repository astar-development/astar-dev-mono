namespace AStarDev.CloudSync.Sync;

internal sealed record GuardedDeletions(IReadOnlyList<LocalChange> Changes, IReadOnlyList<MassDeletionSuspected> Holds);
