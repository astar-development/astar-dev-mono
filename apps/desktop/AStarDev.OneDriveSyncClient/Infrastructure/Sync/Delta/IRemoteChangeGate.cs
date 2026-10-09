using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Accounts;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;

/// <summary>Decides, cheaply, whether the remote tree needs to be walked on this sync pass.</summary>
public interface IRemoteChangeGate
{
    /// <summary>Returns <see cref="SkipRemote"/> only when a stored delta link is still valid, the rules are unchanged, a full enumeration is recent enough and Graph reports no change relevant to the synced scope. Every other situation, including any failure, returns <see cref="WalkRemote"/>.</summary>
    Task<RemoteWalkDecision> DecideAsync(OneDriveAccount account, DriveId driveId, Func<CancellationToken, Task<string>> tokenFactory, IReadOnlyList<SyncRuleEntity> rules, IReadOnlyDictionary<string, SyncedItemEntity> syncedItems, CancellationToken cancellationToken);
}
