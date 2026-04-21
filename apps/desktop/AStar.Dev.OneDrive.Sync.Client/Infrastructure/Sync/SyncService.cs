using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class SyncService(IAuthService authService, IGraphService graphService, IAccountRepository accountRepository, ISyncRepository syncRepository, IDriveStateRepository driveStateRepository, ISyncRuleRepository syncRuleRepository, ISyncedItemRepository syncedItemRepository, ILocalChangeDetector localChangeDetector, IHttpDownloader httpDownloader, IParallelDownloadPipeline parallelDownloadPipeline) : ISyncService
{
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<JobCompletedEventArgs>?  JobCompleted;
    public event EventHandler<SyncConflict>?           ConflictDetected;

    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        Serilog.Log.Information("[SyncService] SyncAccountAsync for {Email}", account.Email);
        RaiseProgress(account.Id.Id, 0, 0, "Authenticating...", SyncState.Syncing);

        var authResult = await authService.AcquireTokenSilentAsync(account.Id.Id, ct);

        if(authResult.IsError)
        {
            RaiseProgress(account.Id.Id, 0, 0, authResult.ErrorMessage ?? "Auth failed", SyncState.Error);
            return;
        }

        if(account.LocalSyncPath is null)
        {
            RaiseProgress(account.Id.Id, 0, 0, "No local sync path configured", SyncState.Error);
            return;
        }

        try
        {
            await SyncAccountInternalAsync(account, authResult.AccessToken!, ct);
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[SyncService] Unhandled error syncing {Email}: {Error}", account.Email, ex.Message);
            RaiseProgress(account.Id.Id, 0, 0, ex.Message, SyncState.Error);
        }
    }

    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        var authResult = await authService.AcquireTokenSilentAsync(conflict.AccountId, ct);

        if(authResult.IsError)
            return;

        var outcome = ConflictResolver.Resolve(policy, conflict.LocalModified, conflict.RemoteModified);

        await ApplyConflictOutcomeAsync(conflict, outcome, authResult.AccessToken!, ct);
        await syncRepository.ResolveConflictAsync(conflict.Id, policy);
    }

    private async Task SyncAccountInternalAsync(OneDriveAccount account, string token, CancellationToken ct)
    {
        string localBasePath = account.LocalSyncPath!.Value;

        var driveState = await driveStateRepository.GetByAccountIdAsync(account.Id, ct)
                         ?? new DriveStateEntity { AccountId = account.Id };

        driveState.LastSyncStartedAt = DateTimeOffset.UtcNow;
        await driveStateRepository.UpsertAsync(driveState, ct);

        var rules = await syncRuleRepository.GetByAccountIdAsync(account.Id, ct);

        if(rules.Count == 0)
        {
            Serilog.Log.Information("[SyncService] No sync rules configured for {Email} — nothing to sync", account.Email);
            RaiseProgress(account.Id.Id, 0, 0, "No folders selected", SyncState.Idle);
            return;
        }

        var syncedItems = await syncedItemRepository.GetAllByAccountAsync(account.Id, ct);

        RaiseProgress(account.Id.Id, 0, 0, "Fetching changes...", SyncState.Syncing);

        DeltaResult delta;
        try
        {
            delta = await graphService.GetDriveRootDeltaAsync(token, driveState.DeltaLink, ct);
        }
        catch(DeltaLinkExpiredException)
        {
            Serilog.Log.Warning("[SyncService] Delta link expired for {Email} — re-enumerating from scratch", account.Email);
            driveState.DeltaLink = null;
            delta = await graphService.GetDriveRootDeltaAsync(token, null, ct);
        }

        Serilog.Log.Information("[SyncService] Delta returned {Count} items for {Email}", delta.Items.Count, account.Email);

        var downloadJobs = await ProcessDeltaItemsAsync(account, token, localBasePath, delta.Items, rules, syncedItems, ct);

        RaiseProgress(account.Id.Id, 0, 0, "Detecting local changes...", SyncState.Syncing);

        var localPathLookup = syncedItems.Values.ToDictionary(i => i.LocalPath, StringComparer.OrdinalIgnoreCase);
        var uploadJobs = localChangeDetector.DetectNewAndModifiedFiles(account.Id.Id, localBasePath, rules, localPathLookup);

        var allJobs = new List<SyncJob>(downloadJobs.Count + uploadJobs.Count);
        allJobs.AddRange(downloadJobs);
        allJobs.AddRange(uploadJobs);

        await DetectAndEnqueueLocalDeletesAsync(account, token, syncedItems, ct);

        if(allJobs.Count > 0)
        {
            RaiseProgress(account.Id.Id, 0, allJobs.Count, $"Syncing {allJobs.Count} file(s)...", SyncState.Syncing);
            await ExecuteJobsAsync(account, token, allJobs, syncedItems, ct);
        }
        else
        {
            RaiseProgress(account.Id.Id, 0, 0, "No changes", SyncState.Idle);
        }

        if(delta.NextDeltaLink is not null)
        {
            driveState.DeltaLink = delta.NextDeltaLink;
            await driveStateRepository.UpsertAsync(driveState, ct);
        }

        var entity = await accountRepository.GetByIdAsync(account.Id, ct);
        if(entity is not null)
        {
            entity.LastSyncedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpsertAsync(entity, ct);
        }

        account.LastSyncedAt = DateTimeOffset.UtcNow;

        Serilog.Log.Information("[SyncService] Sync complete for {Email}", account.Email);
        RaiseProgress(account.Id.Id, 0, 0, "Sync complete", SyncState.Idle);
    }

    private async Task<List<SyncJob>> ProcessDeltaItemsAsync(OneDriveAccount account, string token, string localBasePath, List<DeltaItem> items, List<SyncRuleEntity> rules, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        List<SyncJob> downloadJobs = [];

        foreach(var item in items.TakeWhile(_ => !ct.IsCancellationRequested))
        {
            string remotePath = NormaliseRemotePath(item.RelativePath ?? item.Name);

            if(string.IsNullOrEmpty(remotePath) || remotePath == "/")
                continue;

            if(item.IsDeleted)
            {
                await HandleRemoteDeleteAsync(account.Id, item, syncedItems, ct);
                continue;
            }

            if(item.IsFolder)
            {
                await HandleFolderAsync(account.Id, item, remotePath, localBasePath, syncedItems, ct);
                continue;
            }

            if(!SyncRuleEvaluator.IsIncluded(remotePath, rules))
                continue;

            string localPath = BuildLocalPath(localBasePath, item.RelativePath ?? item.Name);

            syncedItems.TryGetValue(item.Id, out var knownItem);

            if(knownItem is not null && File.Exists(localPath))
            {
                var localModified = new DateTimeOffset(new FileInfo(localPath).LastWriteTimeUtc, TimeSpan.Zero);
                bool isConflict = localModified > knownItem.RemoteModifiedAt.AddSeconds(5);

                if(isConflict)
                {
                    var conflict = BuildConflict(account, item, localPath, localModified);
                    await HandleConflictAsync(account, item, localPath, localModified, conflict, downloadJobs, ct);
                    continue;
                }
            }
            else if(knownItem is null && File.Exists(localPath))
            {
                var localModified = new DateTimeOffset(new FileInfo(localPath).LastWriteTimeUtc, TimeSpan.Zero);
                Serilog.Log.Debug("[SyncService] File exists locally without SyncedItemEntity — treating as synced: {Path}", localPath);
                await syncedItemRepository.UpsertAsync(BuildSyncedItem(account.Id, item, remotePath, localPath), ct);
                syncedItems[item.Id] = BuildSyncedItem(account.Id, item, remotePath, localPath);
                continue;
            }

            downloadJobs.Add(new SyncJob
            {
                AccountId      = account.Id.Id,
                FolderId       = string.Empty,
                RemoteItemId   = item.Id,
                RelativePath   = item.RelativePath ?? item.Name,
                LocalPath      = localPath,
                Direction      = SyncDirection.Download,
                DownloadUrl    = item.DownloadUrl,
                FileSize       = item.Size,
                RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
            });
        }

        return downloadJobs;
    }

    private async Task HandleRemoteDeleteAsync(AccountId accountId, DeltaItem item, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        if(!syncedItems.TryGetValue(item.Id, out var knownItem))
            return;

        string localPath = knownItem.LocalPath;

        if(knownItem.IsFolder)
        {
            if(Directory.Exists(localPath))
            {
                Serilog.Log.Information("[SyncService] Remote folder deleted — removing local: {Path}", localPath);
                Directory.Delete(localPath, recursive: true);
            }
        }
        else
        {
            if(File.Exists(localPath))
            {
                Serilog.Log.Information("[SyncService] Remote file deleted — removing local: {Path}", localPath);
                File.Delete(localPath);
            }
        }

        await syncedItemRepository.DeleteByRemoteIdAsync(accountId, knownItem.RemoteItemId, ct);
        syncedItems.Remove(item.Id);
    }

    private async Task HandleFolderAsync(AccountId accountId, DeltaItem item, string remotePath, string localBasePath, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        string localPath = BuildLocalPath(localBasePath, item.RelativePath ?? item.Name);
        _ = Directory.CreateDirectory(localPath);

        var entity = BuildSyncedItem(accountId, item, remotePath, localPath);
        await syncedItemRepository.UpsertAsync(entity, ct);
        syncedItems[item.Id] = entity;
    }

    private async Task HandleConflictAsync(OneDriveAccount account, DeltaItem item, string localPath, DateTimeOffset localModified, SyncConflict conflict, List<SyncJob> downloadJobs, CancellationToken ct)
    {
        await syncRepository.AddConflictAsync(conflict);
        ConflictDetected?.Invoke(this, conflict);

        var outcome = ConflictResolver.Resolve(account.ConflictPolicy, localModified, item.LastModified ?? DateTimeOffset.MinValue);

        switch(outcome)
        {
            case ConflictOutcome.Skip:
                break;

            case ConflictOutcome.UseRemote:
                downloadJobs.Add(new SyncJob
                {
                    AccountId      = account.Id.Id,
                    FolderId       = string.Empty,
                    RemoteItemId   = item.Id,
                    RelativePath   = item.RelativePath ?? item.Name,
                    LocalPath      = localPath,
                    Direction      = SyncDirection.Download,
                    DownloadUrl    = item.DownloadUrl,
                    FileSize       = item.Size,
                    RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
                });
                break;

            case ConflictOutcome.UseLocal:
                downloadJobs.Add(new SyncJob
                {
                    AccountId      = account.Id.Id,
                    FolderId       = string.Empty,
                    RemoteItemId   = item.Id,
                    RelativePath   = item.RelativePath ?? item.Name,
                    LocalPath      = localPath,
                    Direction      = SyncDirection.Upload,
                    FileSize       = new FileInfo(localPath).Length,
                    RemoteModified = localModified
                });
                break;

            case ConflictOutcome.KeepBoth:
                string newName = ConflictResolver.MakeKeepBothName(localPath, localModified);
                File.Move(localPath, newName);
                downloadJobs.Add(new SyncJob
                {
                    AccountId      = account.Id.Id,
                    FolderId       = string.Empty,
                    RemoteItemId   = item.Id,
                    RelativePath   = item.RelativePath ?? item.Name,
                    LocalPath      = localPath,
                    Direction      = SyncDirection.Download,
                    DownloadUrl    = item.DownloadUrl,
                    FileSize       = item.Size,
                    RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
                });
                break;
        }
    }

    private async Task DetectAndEnqueueLocalDeletesAsync(OneDriveAccount account, string token, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        foreach(var (remoteId, knownItem) in syncedItems)
        {
            if(knownItem.IsFolder) continue;
            if(ct.IsCancellationRequested) break;
            if(File.Exists(knownItem.LocalPath)) continue;

            Serilog.Log.Information("[SyncService] Local file deleted — removing remote: {Path}", knownItem.RemotePath);

            try
            {
                await graphService.DeleteItemAsync(token, remoteId, ct);
                await syncedItemRepository.DeleteByRemoteIdAsync(account.Id, knownItem.RemoteItemId, ct);
            }
            catch(Exception ex)
            {
                Serilog.Log.Error(ex, "[SyncService] Failed to delete remote item {RemoteId}: {Error}", remoteId, ex.Message);
            }
        }
    }

    private async Task ExecuteJobsAsync(OneDriveAccount account, string token, List<SyncJob> jobs, Dictionary<string, SyncedItemEntity> syncedItems, CancellationToken ct)
    {
        await syncRepository.EnqueueJobsAsync(jobs);

        var successfulJobs = new System.Collections.Concurrent.ConcurrentBag<SyncJob>();

        await parallelDownloadPipeline.RunAsync(
            jobs,
            token,
            args => SyncProgressChanged?.Invoke(this, args),
            args =>
            {
                if(args.Job.State == SyncJobState.Completed)
                    successfulJobs.Add(args.Job);

                JobCompleted?.Invoke(this, args);
            },
            account.Id.Id,
            string.Empty,
            ct: ct);

        foreach(var job in successfulJobs)
        {
            if(job.Direction == SyncDirection.Download)
            {
                var remotePath = NormaliseRemotePath(job.RelativePath);
                var entity = new SyncedItemEntity
                {
                    AccountId       = account.Id,
                    RemoteItemId    = new OneDriveItemId(job.RemoteItemId),
                    RemoteParentId  = string.Empty,
                    RemotePath      = remotePath,
                    LocalPath       = job.LocalPath,
                    IsFolder        = false,
                    RemoteModifiedAt = job.RemoteModified
                };
                await syncedItemRepository.UpsertAsync(entity, ct);
                syncedItems[job.RemoteItemId] = entity;
            }
            else if(job.Direction == SyncDirection.Upload && job.UploadedRemoteItemId is not null)
            {
                var remotePath = NormaliseRemotePath(job.RelativePath);
                var localModified = new DateTimeOffset(new FileInfo(job.LocalPath).LastWriteTimeUtc, TimeSpan.Zero);
                var entity = new SyncedItemEntity
                {
                    AccountId       = account.Id,
                    RemoteItemId    = new OneDriveItemId(job.UploadedRemoteItemId),
                    RemoteParentId  = string.Empty,
                    RemotePath      = remotePath,
                    LocalPath       = job.LocalPath,
                    IsFolder        = false,
                    RemoteModifiedAt = localModified
                };
                await syncedItemRepository.UpsertAsync(entity, ct);
                syncedItems[job.UploadedRemoteItemId] = entity;
            }
        }
    }

    private async Task ApplyConflictOutcomeAsync(SyncConflict conflict, ConflictOutcome outcome, string accessToken, CancellationToken ct)
    {
        switch(outcome)
        {
            case ConflictOutcome.UseRemote:
                string downloadUrl = await graphService.GetDownloadUrlAsync(accessToken, conflict.RemoteItemId, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"No download URL could be resolved for conflict item '{conflict.RelativePath}' (itemId={conflict.RemoteItemId}).");

                await httpDownloader.DownloadAsync(downloadUrl, conflict.LocalPath, conflict.RemoteModified, ct: ct);
                break;

            case ConflictOutcome.KeepBoth:
                string keepBothName = ConflictResolver.MakeKeepBothName(conflict.LocalPath, conflict.LocalModified);
                if(File.Exists(conflict.LocalPath))
                    File.Move(conflict.LocalPath, keepBothName);
                break;
        }
    }

    private static SyncConflict BuildConflict(OneDriveAccount account, DeltaItem item, string localPath, DateTimeOffset localModified)
        => new()
        {
            AccountId      = account.Id.Id,
            FolderId       = string.Empty,
            RemoteItemId   = item.Id,
            RelativePath   = item.RelativePath ?? item.Name,
            LocalPath      = localPath,
            LocalModified  = localModified,
            RemoteModified = item.LastModified ?? DateTimeOffset.MinValue,
            LocalSize      = new FileInfo(localPath).Length,
            RemoteSize     = item.Size
        };

    private static SyncedItemEntity BuildSyncedItem(AccountId accountId, DeltaItem item, string remotePath, string localPath)
        => new()
        {
            AccountId        = accountId,
            RemoteItemId     = new OneDriveItemId(item.Id),
            RemoteParentId   = item.ParentId ?? string.Empty,
            RemotePath       = remotePath,
            LocalPath        = localPath,
            IsFolder         = item.IsFolder,
            RemoteModifiedAt = item.LastModified ?? DateTimeOffset.MinValue
        };

    private static string NormaliseRemotePath(string? relativePath)
        => string.IsNullOrEmpty(relativePath) ? "/" : $"/{relativePath.TrimStart('/')}";

    private static string BuildLocalPath(string localBasePath, string relativePath)
        => Path.Combine(localBasePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private void RaiseProgress(string accountId, int completed, int total, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, string.Empty, completed, total, currentFile, syncState));
}
