using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

public sealed class SyncService(IAuthService authService, IGraphService graphService, IAccountRepository accountRepository, ISyncRepository syncRepository, ILocalChangeDetector localChangeDetector, IHttpDownloader httpDownloader, IParallelDownloadPipeline parallelDownloadPipeline) : ISyncService
{
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<JobCompletedEventArgs>?  JobCompleted;
    public event EventHandler<SyncConflict>?           ConflictDetected;

    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        Serilog.Log.Information("[SyncService] SyncAccountAsync called for {Email}, LocalSyncPath={Path}, Folders={Count}", account.Email, account.LocalSyncPath?.Value, account.SelectedFolderIds.Count);

        RaiseProgress(account.Id.Id, string.Empty, 0, 0, "Authenticating...", SyncState.Syncing);
        var authResult = await authService.AcquireTokenSilentAsync(account.Id.Id, ct);

        Serilog.Log.Information("[SyncService] Auth result: IsError={IsError}, Error={Error}", authResult.IsError, authResult.ErrorMessage ?? "none");

        if(authResult.IsError)
        {
            RaiseProgress(account.Id.Id, string.Empty, 0, 0, authResult.ErrorMessage ?? "Auth failed", SyncState.Error);

            return;
        }

        string token = authResult.AccessToken!;

        Serilog.Log.Information("[SyncService] LocalSyncPath check: '{Path}' IsNull={IsNull}", account.LocalSyncPath?.Value, account.LocalSyncPath is null);
        if(account.LocalSyncPath is null)
        {
            RaiseProgress(account.Id.Id, string.Empty, 0, 0, "No local sync path configured", SyncState.Error);

            return;
        }

        var excludedFolderIds = account.ExplicitlyExcludedFolderIds
            .Select(f => f.Id)
            .ToHashSet();

        Serilog.Log.Information("[SyncService] About to loop {Count} folders", account.SelectedFolderIds.Count);
        foreach(OneDriveFolderId folderId in account.SelectedFolderIds.TakeWhile(_ => !ct.IsCancellationRequested))
        {
            await SyncFolderAsync(account, token, folderId, excludedFolderIds, ct);
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

    private async Task SyncFolderAsync(OneDriveAccount account, string token, OneDriveFolderId folderId, IReadOnlySet<string> excludedFolderIds, CancellationToken ct)
    {
        Serilog.Log.Information("Starting sync for account {AccountId}, folder {FolderId}", account.Id.Id, folderId.Id);

        try
        {
            RaiseProgress(account.Id.Id, folderId.Id, 0, 0, "Getting account details", SyncState.Syncing);
            var entity = await accountRepository.GetByIdAsync(account.Id, CancellationToken.None);
            var folderEntity = entity?.SyncFolders.FirstOrDefault(f => f.FolderId == folderId);

            string folderRelativePath = folderEntity?.FolderName ?? string.Empty;
            string? deltaLink = folderEntity?.DeltaLink;

            RaiseProgress(account.Id.Id, folderId.Id, 0, 0, "Fetching changes\u2026", SyncState.Syncing);

            var (delta, allJobs) = await ProcessDownloadDeltasAsync(account, token, folderId, folderRelativePath, deltaLink, excludedFolderIds, ct);

            DetectLocalChanges(account, folderId, folderEntity, allJobs);

            if(allJobs.Count > 0)
            {
                RaiseProgress(account.Id.Id, folderId.Id, 0, 0, $"Queuing {allJobs.Count} file(s) for sync...", SyncState.Syncing);
                await ProcessJobQueueAsync(account, token, allJobs, ct);

                Serilog.Log.Information("[SyncFolder] ProcessJobQueueAsync completed for {FolderId}", folderId.Id);
            }
            else
            {
                await ReportNoRemoteOrLocalChangesAsync(account, folderId, entity);
            }

            if(delta.NextDeltaLink is not null)
            {
                await accountRepository.UpdateDeltaLinkAsync(account.Id, folderId, delta.NextDeltaLink, CancellationToken.None);
            }

            if(entity is not null)
            {
                entity.LastSyncedAt = DateTimeOffset.UtcNow;
                await accountRepository.UpsertAsync(entity, CancellationToken.None);
            }

            account.LastSyncedAt = DateTimeOffset.UtcNow;

            Serilog.Log.Information("Finished sync for account {AccountId}, folder {FolderId}", account.Id.Id, folderId.Id);
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[SyncService] Error syncing folder {FolderId}: {Error}", folderId.Id, ex.Message);
            RaiseProgress(account.Id.Id, folderId.Id, 0, 0, ex.Message, SyncState.Error);
        }
    }

    private async Task ReportNoRemoteOrLocalChangesAsync(OneDriveAccount account, OneDriveFolderId folderId, AccountEntity? entity)
    {
        account.LastSyncedAt = DateTimeOffset.UtcNow;

        if(entity is not null)
        {
            entity.LastSyncedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpsertAsync(entity, CancellationToken.None);
        }

        RaiseProgress(account.Id.Id, folderId.Id, 0, 0, "No changes", SyncState.Idle);
    }

    private void DetectLocalChanges(OneDriveAccount account, OneDriveFolderId folderId, SyncFolderEntity? folderEntity, List<SyncJob> allJobs)
    {
        if(account.LocalSyncPath is null) return;

        string folderLocalPath = Path.Combine(account.LocalSyncPath.Value, folderEntity?.FolderName ?? string.Empty);

        if(!Directory.Exists(folderLocalPath)) return;

        Serilog.Log.Information("[SyncFolder] Local path={LocalPath}, FolderName={FolderName}, LastSyncedAt={LastSync}", account.LocalSyncPath.Value, folderEntity?.FolderName ?? "(null)", account.LastSyncedAt);
        Serilog.Log.Information("[SyncFolder] Scanning for uploads at: {FolderLocalPath}", folderLocalPath);

        var uploadJobs = localChangeDetector.DetectChanges(account.Id.Id, folderId.Id, folderLocalPath, remoteFolderPath: string.Empty, account.LastSyncedAt);

        if(uploadJobs.Count <= 0) return;

        Serilog.Log.Information("[SyncService] Found {Count} local changes to upload", uploadJobs.Count);
        allJobs.AddRange(uploadJobs);
    }

    private async Task<(DeltaResult delta, List<SyncJob> allJobs)> ProcessDownloadDeltasAsync(OneDriveAccount account, string token, OneDriveFolderId folderId, string folderRelativePath, string? deltaLink, IReadOnlySet<string> excludedFolderIds, CancellationToken ct)
    {
        var delta = await graphService.GetDeltaAsync(token, folderId.Id, folderRelativePath, deltaLink, excludedFolderIds, ct);

        Serilog.Log.Information("[SyncService] Delta for folder {FolderId}: {Count} items, deltaLink={HasDelta}", folderId.Id, delta.Items.Count, delta.NextDeltaLink is not null);

        List<SyncJob> allJobs = [];

        if(delta.Items.Count <= 0) return (delta, allJobs);

        var downloadJobs = BuildJobs(account, folderId, delta.Items);
        var (cleanJobs, conflicts) = ClassifyJobs(account, downloadJobs);

        foreach(var conflict in conflicts)
        {
            await syncRepository.AddConflictAsync(conflict);
            ConflictDetected?.Invoke(this, conflict);
        }

        allJobs.AddRange(cleanJobs);

        return (delta, allJobs);
    }

    private static List<SyncJob> BuildJobs(OneDriveAccount account, OneDriveFolderId folderId, List<DeltaItem> items)
    {
        List<SyncJob> jobs = [];

        foreach(var item in items)
        {
            if(item.IsFolder)
                continue;

            string relativePath = item.RelativePath ?? item.Name;
            string localPath    = Path.Combine(account.LocalSyncPath!.Value, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if(item.IsDeleted)
            {
                if(File.Exists(localPath))
                {
                    jobs.Add(new SyncJob
                    {
                        AccountId    = account.Id.Id,
                        FolderId     = folderId.Id,
                        RemoteItemId = item.Id,
                        RelativePath = relativePath,
                        LocalPath    = localPath,
                        Direction    = SyncDirection.Delete,
                        RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
                    });
                }
            }
            else
            {
                jobs.Add(new SyncJob
                {
                    AccountId    = account.Id.Id,
                    FolderId     = folderId.Id,
                    RemoteItemId = item.Id,
                    RelativePath = relativePath,
                    LocalPath    = localPath,
                    Direction    = SyncDirection.Download,
                    DownloadUrl  = item.DownloadUrl,
                    FileSize     = item.Size,
                    RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
                });
            }
        }

        return jobs;
    }

    private static (List<SyncJob> Clean, List<SyncConflict> Conflicts) ClassifyJobs(OneDriveAccount account, List<SyncJob> jobs)
    {
        List<SyncJob>      clean     = [];
        List<SyncConflict> conflicts = [];

        foreach(var job in jobs)
        {
            if(job.Direction == SyncDirection.Delete || !File.Exists(job.LocalPath))
            {
                clean.Add(job);
                continue;
            }

            var localInfo     = new FileInfo(job.LocalPath);
            var localModified = new DateTimeOffset(
                localInfo.LastWriteTimeUtc, TimeSpan.Zero);

            bool isConflict = localModified > job.RemoteModified.AddSeconds(-5);

            if(!isConflict)
            {
                clean.Add(job);
                continue;
            }

            var outcome = ConflictResolver.Resolve(account.ConflictPolicy, localModified, job.RemoteModified);

            switch(outcome)
            {
                case ConflictOutcome.Skip:
                    conflicts.Add(new SyncConflict
                    {
                        AccountId      = account.Id.Id,
                        FolderId       = job.FolderId,
                        RemoteItemId   = job.RemoteItemId,
                        RelativePath   = job.RelativePath,
                        LocalPath      = job.LocalPath,
                        LocalModified  = localModified,
                        RemoteModified = job.RemoteModified,
                        LocalSize      = localInfo.Length,
                        RemoteSize     = job.FileSize
                    });
                    break;

                case ConflictOutcome.UseRemote:
                    clean.Add(job);
                    break;

                case ConflictOutcome.UseLocal:
                    clean.Add(job with { Direction = SyncDirection.Upload });
                    break;

                case ConflictOutcome.KeepBoth:
                    string newName = ConflictResolver.MakeKeepBothName(job.LocalPath, localModified);
                    File.Move(job.LocalPath, newName);
                    clean.Add(job);
                    break;
                default:
                    throw new InvalidDataException($"Outcome of type '{outcome}' is not supported.");
            }
        }

        return (clean, conflicts);
    }

    private async Task ProcessJobQueueAsync(OneDriveAccount account, string accessToken, List<SyncJob> jobs, CancellationToken ct)
    {
        if(jobs.Count == 0)
            return;

        int downloads = jobs.Count(j => j.Direction == SyncDirection.Download);
        int uploads   = jobs.Count(j => j.Direction == SyncDirection.Upload);
        int deletes   = jobs.Count(j => j.Direction == SyncDirection.Delete);

        Serilog.Log.Information("[SyncService] Processing {Total} jobs for {Email}: {D} downloads, {U} uploads, {Del} deletes", jobs.Count, account.Email, downloads, uploads, deletes);

        await syncRepository.EnqueueJobsAsync(jobs);

        await parallelDownloadPipeline.RunAsync(jobs, accessToken, args => SyncProgressChanged?.Invoke(this, args), args => JobCompleted?.Invoke(this, args), account.Id.Id, jobs.FirstOrDefault()?.FolderId ?? string.Empty, ct: ct);
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

    private void RaiseProgress(string accountId, string folderId, int completed, int total, string currentFile, SyncState syncState)
        => SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(accountId, folderId, completed, total, currentFile, syncState));
}
