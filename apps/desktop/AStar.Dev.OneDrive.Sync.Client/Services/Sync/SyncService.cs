using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

public sealed class SyncService(IAuthService authService, IGraphService graphService, IAccountRepository accountRepository, ISyncRepository syncRepository) : ISyncService
{
    private readonly LocalChangeDetector _changeDetector = new();

    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<JobCompletedEventArgs>?  JobCompleted;
    public event EventHandler<SyncConflict>?           ConflictDetected;

    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        Serilog.Log.Information("[SyncService] SyncAccountAsync called for {Email}, LocalSyncPath={Path}, Folders={Count}", account.Email, account.LocalSyncPath, account.SelectedFolderIds.Count);

        //Dashboard.UpdateAccountSyncState(e.AccountId, card); // need this or similar to immediately reflect "Syncing" state in UI when sync starts, otherwise there can be a long delay before UI updates, which is bad UX
        var authResult = await authService.AcquireTokenSilentAsync(account.Id, ct);
        SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(account.Id, folderId: string.Empty, completed: 0, total: 0, currentFile: "TEST TEST TEST !!!", SyncState.Syncing));

        Serilog.Log.Information("[SyncService] Auth result: IsError={IsError}, Error={Error}", authResult.IsError, authResult.ErrorMessage ?? "none");

        if(authResult.IsError)
        {
            RaiseProgress(account.Id, string.Empty, 0, 0, authResult.ErrorMessage ?? "Auth failed", SyncState.Error);
            return;
        }

        string token = authResult.AccessToken!;

        Serilog.Log.Information("[SyncService] LocalSyncPath check: '{Path}' IsEmpty={IsEmpty}", account.LocalSyncPath, string.IsNullOrEmpty(account.LocalSyncPath));
        if(string.IsNullOrEmpty(account.LocalSyncPath))
        {
            RaiseProgress(account.Id, string.Empty, 0, 0, "No local sync path configured", SyncState.Error);
            return;
        }

        Serilog.Log.Information("[SyncService] About to loop {Count} folders", account.SelectedFolderIds.Count);
        foreach(string folderId in account.SelectedFolderIds)
        {
            if(ct.IsCancellationRequested)
                break;
            await SyncFolderAsync(account, token, folderId, ct);
        }
    }

    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        var authResult = await authService.AcquireTokenSilentAsync(conflict.AccountId, ct);

        if(authResult.IsError)
            return;

        var outcome = ConflictResolver.Resolve(policy, conflict.LocalModified, conflict.RemoteModified);

        await ApplyConflictOutcomeAsync(conflict, outcome, ct);

        await syncRepository.ResolveConflictAsync(conflict.Id, policy);
    }

    private async Task SyncFolderAsync(OneDriveAccount account, string token, string folderId, CancellationToken ct)
    {
        Serilog.Log.Information("Starting sync for account {AccountId}, folder {FolderId}", account.Id, folderId);

        try
        {
            RaiseProgress(account.Id, string.Empty, 0, 0, "Getting account details", SyncState.Syncing);
            var entity = await accountRepository.GetByIdAsync(account.Id);
            var folderEntity = entity?.SyncFolders.FirstOrDefault(f => f.FolderId == folderId);

            string? deltaLink = folderEntity?.DeltaLink;

            RaiseProgress(account.Id, folderId, 0, 0, "Fetching changes\u2026", SyncState.Syncing);

            (var delta, var allJobs) = await ProcessPownloadDeltas(account, token, folderId, deltaLink, ct);

            DetectLocalChanges(account, folderId, folderEntity, allJobs);

            if(allJobs.Count > 0)
            {
                await ProcessJobQueueAsync(account, token, allJobs, ct);

                Serilog.Log.Information("[SyncFolder] ProcessJobQueueAsync completed for {FolderId}", folderId);
            }
            else
            {
                await ReportNoRemoteOrLocalChanges(account, folderId, entity);
            }

            if(delta.NextDeltaLink is not null)
            {
                await accountRepository.UpdateDeltaLinkAsync(account.Id, folderId, delta.NextDeltaLink);
            }

            if(entity is not null)
            {
                entity.LastSyncedAt = DateTimeOffset.UtcNow;
                await accountRepository.UpsertAsync(entity);
            }

            account.LastSyncedAt = DateTimeOffset.UtcNow;

            Serilog.Log.Information("Finished sync for account {AccountId}, folder {FolderId}", account.Id, folderId);
        }
        catch(Exception ex)
        {
            Serilog.Log.Error(ex, "[SyncService] Error syncing folder {FolderId}: {Error}", folderId, ex.Message);
            RaiseProgress(account.Id, folderId, 0, 0, ex.Message, SyncState.Error);
        }
    }

    private async Task ReportNoRemoteOrLocalChanges(OneDriveAccount account, string folderId, AccountEntity? entity)
    {
        account.LastSyncedAt = DateTimeOffset.UtcNow;

        if(entity is not null)
        {
            entity.LastSyncedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpsertAsync(entity);
        }

        RaiseProgress(account.Id, folderId, 0, 0, "No changes", SyncState.Idle);
    }

    private void DetectLocalChanges(OneDriveAccount account, string folderId, SyncFolderEntity? folderEntity, List<SyncJob> allJobs)
    {
        if(!string.IsNullOrEmpty(account.LocalSyncPath))
        {
            string folderLocalPath = Path.Combine(account.LocalSyncPath, folderEntity?.FolderName ?? string.Empty);

            if(Directory.Exists(folderLocalPath))
            {
                Serilog.Log.Information("[SyncFolder] Local path={LocalPath}, FolderName={FolderName}, LastSyncedAt={LastSync}", account.LocalSyncPath, folderEntity?.FolderName ?? "(null)", account.LastSyncedAt);

                Serilog.Log.Information("[SyncFolder] Scanning for uploads at: {FolderLocalPath}", folderLocalPath);

                var uploadJobs = _changeDetector.DetectChanges(account.Id, folderId, folderLocalPath, remoteFolderPath: string.Empty,  account.LastSyncedAt);

                if(uploadJobs.Count > 0)
                {
                    Serilog.Log.Information("[SyncService] Found {Count} local changes to upload", uploadJobs.Count);
                    allJobs.AddRange(uploadJobs);
                }
            }
        }
    }

    private async Task<(DeltaResult delta, List<SyncJob> allJobs)> ProcessPownloadDeltas(OneDriveAccount account, string token, string folderId, string? deltaLink, CancellationToken ct)
    {
        var delta = await graphService.GetDeltaAsync(token, folderId, deltaLink, ct);

        Serilog.Log.Information("[SyncService] Delta for folder {FolderId}: {Count} items, deltaLink={HasDelta}", folderId, delta.Items.Count, delta.NextDeltaLink is not null);

        List<SyncJob> allJobs = [];

        if(delta.Items.Count > 0)
        {
            var downloadJobs = BuildJobs(account, folderId, delta.Items);
            (var cleanJobs, var conflicts) = await ClassifyJobsAsync(account, downloadJobs);

            foreach(var conflict in conflicts)
            {
                await syncRepository.AddConflictAsync(conflict);
                ConflictDetected?.Invoke(this, conflict);
            }

            allJobs.AddRange(cleanJobs);
        }

        return (delta, allJobs);
    }

    private static List<SyncJob> BuildJobs(OneDriveAccount account, string folderId, List<DeltaItem> items)
    {
        List<SyncJob> jobs = [];

        foreach(var item in items)
        {
            if(item.IsFolder)
                continue;

            string relativePath = item.RelativePath ?? item.Name;
            string localPath    = Path.Combine(account.LocalSyncPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if(item.IsDeleted)
            {
                if(File.Exists(localPath))
                {
                    jobs.Add(new SyncJob
                    {
                        AccountId = account.Id,
                        FolderId = folderId,
                        RemoteItemId = item.Id,
                        RelativePath = relativePath,
                        LocalPath = localPath,
                        Direction = SyncDirection.Delete,
                        RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
                    });
                }
            }
            else
            {
                jobs.Add(new SyncJob
                {
                    AccountId = account.Id,
                    FolderId = folderId,
                    RemoteItemId = item.Id,
                    RelativePath = relativePath,
                    LocalPath = localPath,
                    Direction = SyncDirection.Download,
                    DownloadUrl = item.DownloadUrl,
                    FileSize = item.Size,
                    RemoteModified = item.LastModified ?? DateTimeOffset.MinValue
                });
            }
        }

        return jobs;
    }

    private static async Task<(List<SyncJob> Clean, List<SyncConflict> Conflicts)> ClassifyJobsAsync(OneDriveAccount account, List<SyncJob> jobs)
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
                        AccountId = account.Id,
                        FolderId = job.FolderId,
                        RemoteItemId = job.RemoteItemId,
                        RelativePath = job.RelativePath,
                        LocalPath = job.LocalPath,
                        LocalModified = localModified,
                        RemoteModified = job.RemoteModified,
                        LocalSize = localInfo.Length,
                        RemoteSize = job.FileSize
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
            }
        }

        await Task.CompletedTask;
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

        var pipeline = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 8);

        await pipeline.RunAsync(jobs, accessToken, args => SyncProgressChanged?.Invoke(this, args), args => JobCompleted?.Invoke(this, args), account.Id, jobs.FirstOrDefault()?.FolderId ?? string.Empty, ct: ct);
    }

    private static async Task ApplyConflictOutcomeAsync(SyncConflict conflict, ConflictOutcome outcome, CancellationToken ct)
    {
        switch(outcome)
        {
            case ConflictOutcome.UseRemote:
                var job = new SyncJob
                {
                    AccountId      = conflict.AccountId,
                    FolderId       = conflict.FolderId,
                    RemoteItemId   = conflict.RemoteItemId,
                    RelativePath   = conflict.RelativePath,
                    LocalPath      = conflict.LocalPath,
                    Direction      = SyncDirection.Download,
                    RemoteModified = conflict.RemoteModified
                };

                await new HttpDownloader().DownloadAsync(job.DownloadUrl ?? string.Empty, job.LocalPath, job.RemoteModified, ct: ct);
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
