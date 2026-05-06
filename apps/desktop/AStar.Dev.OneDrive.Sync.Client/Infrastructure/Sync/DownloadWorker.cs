using System.IO.Abstractions;
using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// A single download worker that drains jobs from a
/// <see cref="ChannelReader{T}"/> and executes them.
/// Multiple workers run concurrently — one per degree of parallelism.
/// </summary>
public sealed class DownloadWorker(int workerId, IHttpDownloader downloader, IGraphService graphService, ISyncRepository syncRepository, IFileSystem fileSystem)
{
    /// <summary>Runs the worker, draining all jobs from <paramref name="reader"/> until the channel is complete or <paramref name="ct"/> is cancelled.</summary>
    public async Task RunAsync(ChannelReader<SyncJob> reader, string accessToken, Action<SyncJob, bool, string?> onJobComplete, CancellationToken ct)
    {
        await foreach(var job in reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            Serilog.Log.Debug(
                "[Worker {Id}] Processing {JobType} {Path}",
                workerId, job.GetType().Name, job.Target.RelativePath);

            await syncRepository.UpdateJobStateAsync(
                job.Status.Id, SyncJobState.InProgress).ConfigureAwait(false);

            string? error   = null;
            bool     success = false;
            var currentJob = job;

            try
            {
                currentJob = await ExecuteJobAsync(job, accessToken, ct).ConfigureAwait(false);
                success = true;
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Completed).ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Queued).ConfigureAwait(false);
                throw;
            }
            catch(Exception ex)
            {
                error = ex.Message;
                Serilog.Log.Error(ex, "[Worker {Id}] EXCEPTION type={Type} message={Error} path={Path}", workerId, ex.GetType().Name, ex.Message, job.Target.LocalPath);
                await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                onJobComplete(currentJob, success, error);
            }
        }
    }

    private async Task<SyncJob> ExecuteJobAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        switch(job)
        {
            case DownloadSyncJob downloadJob:
                string downloadUrl = await ResolveDownloadUrlAsync(downloadJob, accessToken, ct);
                await downloader.DownloadAsync(downloadUrl, downloadJob.Target.LocalPath, downloadJob.Metadata.RemoteModified, ct: ct).ConfigureAwait(false);

                return downloadJob;

            case UploadSyncJob uploadJob:
                string uploadedRemoteItemId = await graphService.UploadFileAsync(accessToken, uploadJob.Target.LocalPath, uploadJob.Target.RelativePath, parentFolderId: uploadJob.Remote.FolderId.Id, ct: ct).ConfigureAwait(false);
                Serilog.Log.Information("[Worker {Id}] Uploaded {Path}", workerId, uploadJob.Target.RelativePath);

                return uploadJob with { UploadedRemoteItemId = uploadedRemoteItemId };

            case DeleteSyncJob deleteJob:
                if(fileSystem.File.Exists(deleteJob.Target.LocalPath))
                    fileSystem.File.Delete(deleteJob.Target.LocalPath);

                return deleteJob;

            default:
                return job;
        }
    }

    private async Task<string> ResolveDownloadUrlAsync(DownloadSyncJob job, string accessToken, CancellationToken ct)
    {
        if(job.DownloadUrl is not null)
            return job.DownloadUrl;

        Serilog.Log.Debug("[Worker {Id}] DownloadUrl absent for {Path} — fetching on-demand", workerId, job.Target.RelativePath);

        var url = await graphService.GetDownloadUrlAsync(accessToken, job.Remote.RemoteItemId.Id, ct).ConfigureAwait(false);

        return url ?? throw new InvalidOperationException($"No download URL could be resolved for '{job.Target.RelativePath}' (itemId={job.Remote.RemoteItemId.Id}).");
    }
}
