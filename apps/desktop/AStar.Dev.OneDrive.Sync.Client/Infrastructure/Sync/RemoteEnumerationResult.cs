using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Carries the raw output of a single remote-enumeration pass.
/// Downstream processors (<see cref="IDownloadJobBuilder"/>, deletion detectors) consume this result.
/// </summary>
public sealed record RemoteEnumerationResult(IReadOnlyList<DeltaItem> DeltaItems, IReadOnlySet<string> SeenRemoteIds, Dictionary<string, SyncedItemEntity> SyncedItems, IReadOnlyList<SyncRuleEntity> Rules, bool HadNoRules = false);
