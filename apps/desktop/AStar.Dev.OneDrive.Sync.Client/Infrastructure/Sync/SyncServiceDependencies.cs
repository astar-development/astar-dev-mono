namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Groups the five sync-pass collaborators injected into <see cref="SyncPassOrchestrator"/>
/// to keep its constructor within the parameter-count guideline.
/// </summary>
public sealed record SyncServiceDependencies(IRemoteFolderEnumerator RemoteFolderEnumerator, IRemoteDeletionDetector RemoteDeletionDetector, ILocalDeletionDetector LocalDeletionDetector, ILocalChangeDetector LocalChangeDetector, ISyncJobExecutor JobExecutor);
