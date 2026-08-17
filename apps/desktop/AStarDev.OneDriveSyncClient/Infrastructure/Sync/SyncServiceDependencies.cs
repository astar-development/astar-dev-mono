using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Detection;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync;

/// <summary>
/// Groups the sync-pass collaborators injected into <see cref="SyncPassOrchestrator"/>
/// to keep its constructor within the parameter-count guideline.
/// </summary>
public sealed record SyncServiceDependencies(IRemoteFolderEnumerator RemoteFolderEnumerator, IRemoteDeletionDetector RemoteDeletionDetector, ILocalDeletionDetector LocalDeletionDetector, ILocalChangeDetector LocalChangeDetector, ISyncJobExecutor JobExecutor, IDownloadJobBuilder DownloadJobBuilder);
