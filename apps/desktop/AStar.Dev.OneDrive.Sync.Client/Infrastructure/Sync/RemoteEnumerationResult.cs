using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Carries the output of a single remote-enumeration pass.
/// Consumed by deletion detectors and the job executor.
/// </summary>
public sealed record RemoteEnumerationResult(IReadOnlyList<SyncJob> DownloadJobs, IReadOnlySet<string> SeenRemoteIds, Dictionary<string, SyncedItemEntity> SyncedItems, IReadOnlyList<SyncRuleEntity> Rules, bool HadNoRules = false);
