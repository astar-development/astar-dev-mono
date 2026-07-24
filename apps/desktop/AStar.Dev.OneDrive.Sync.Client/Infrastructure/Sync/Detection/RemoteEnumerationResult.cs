using System.Collections.Concurrent;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Detection;

/// <summary>
/// Carries the raw output of a single remote-enumeration pass.
/// Downstream processors (<see cref="IDownloadJobBuilder"/>, deletion detectors) consume this result.
/// </summary>
public sealed record RemoteEnumerationResult(IReadOnlyList<DeltaItem> DeltaItems, IReadOnlySet<string> SeenRemoteIds, ConcurrentDictionary<string, SyncedItemEntity> SyncedItems, IReadOnlyList<SyncRuleEntity> Rules, bool HadNoRules = false);
