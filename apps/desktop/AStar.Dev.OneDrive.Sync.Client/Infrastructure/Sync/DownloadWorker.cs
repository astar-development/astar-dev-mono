using System.IO.Abstractions;
using System.Threading.Channels;
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
                "[Worker {Id}] Processing {Direction} {Path}",
                workerId, job.Direction, job.RelativePath);

            await syncRepository.UpdateJobStateAsync(
                job.Id, SyncJobState.InProgress).ConfigureAwait(false);

            string? error   = null;
            bool     success = false;
            var currentJob = job;

            try
            {
                currentJob = await ExecuteJobAsync(job, accessToken, ct).ConfigureAwait(false);
                success = true;
                await syncRepository.UpdateJobStateAsync(job.Id, SyncJobState.Completed).ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                await syncRepository.UpdateJobStateAsync(job.Id, SyncJobState.Queued).ConfigureAwait(false);
                throw;
            }
            catch(Exception ex)
            {
                error = ex.Message;
                Serilog.Log.Error(ex, "[Worker {Id}] EXCEPTION type={Type} message={Error} path={Path}", workerId, ex.GetType().Name, ex.Message, job.LocalPath);
                await syncRepository.UpdateJobStateAsync(job.Id, SyncJobState.Failed, ex.Message).ConfigureAwait(false);
            }
            finally
            {
                onJobComplete(currentJob, success, error);
            }
        }
    }

    private async Task<SyncJob> ExecuteJobAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        switch(job.Direction)
        {
            case SyncDirection.Download:
                string downloadUrl = await ResolveDownloadUrlAsync(job, accessToken, ct);

                await downloader.DownloadAsync(
                    downloadUrl,
                    job.LocalPath,
                    job.RemoteModified,
                    ct: ct).ConfigureAwait(false);

                return job;

            case SyncDirection.Upload:
                string remotePath = job.DownloadUrl ?? job.RelativePath;
                string uploadedRemoteItemId = await graphService.UploadFileAsync(accessToken, job.LocalPath, remotePath, parentFolderId: job.FolderId, ct: ct).ConfigureAwait(false);

                Serilog.Log.Information("[Worker {Id}] Uploaded {Path}", workerId, job.RelativePath);

                return job with { UploadedRemoteItemId = uploadedRemoteItemId };

            case SyncDirection.Delete:
                if(fileSystem.File.Exists(job.LocalPath))
                    fileSystem.File.Delete(job.LocalPath);

                return job;

            default:
                return job;
        }
    }

    private async Task<string> ResolveDownloadUrlAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        if(job.DownloadUrl is not null)
            return job.DownloadUrl;

        Serilog.Log.Debug("[Worker {Id}] DownloadUrl absent for {Path} — fetching on-demand", workerId, job.RelativePath);

        var url = await graphService.GetDownloadUrlAsync(accessToken, job.RemoteItemId, ct).ConfigureAwait(false);

        return url ?? throw new InvalidOperationException($"No download URL could be resolved for '{job.RelativePath}' (itemId={job.RemoteItemId}).");
    }
}
