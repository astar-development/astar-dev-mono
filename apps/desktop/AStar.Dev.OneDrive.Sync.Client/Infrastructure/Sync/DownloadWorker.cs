using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// A single download worker that drains jobs from a
/// <see cref="ChannelReader{T}"/> and executes them.
/// Multiple workers run concurrently — one per degree of parallelism.
/// </summary>
public sealed class DownloadWorker(int workerId, IHttpDownloader downloader, IGraphService graphService, ISyncRepository syncRepository)
{
    public async Task RunAsync(ChannelReader<SyncJob> reader, string accessToken, Action<SyncJob, bool, string?> onJobComplete, CancellationToken ct)
    {
        await foreach(var job in reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            Serilog.Log.Debug(
                "[Worker {Id}] Processing {Direction} {Path}",
                workerId, job.Direction, job.RelativePath);

            await syncRepository.UpdateJobStateAsync(
                job.Id, SyncJobState.InProgress);

            string? error   = null;
            bool     success = false;

            try
            {
                await ExecuteJobAsync(job, accessToken, ct);
                success = true;
                await syncRepository.UpdateJobStateAsync(job.Id, SyncJobState.Completed);
            }
            catch(OperationCanceledException)
            {
                await syncRepository.UpdateJobStateAsync(job.Id, SyncJobState.Queued);
                throw;
            }
            catch(Exception ex)
            {
                error = ex.Message;
                Serilog.Log.Error(ex, "[Worker {Id}] EXCEPTION type={Type} message={Error} path={Path}", workerId, ex.GetType().Name, ex.Message, job.LocalPath);
                await syncRepository.UpdateJobStateAsync(job.Id, SyncJobState.Failed, ex.Message);
            }
            finally
            {
                onJobComplete(job, success, error);
            }
        }
    }

    private async Task ExecuteJobAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        switch(job.Direction)
        {
            case SyncDirection.Download:
                string downloadUrl = await ResolveDownloadUrlAsync(job, accessToken, ct);

                await downloader.DownloadAsync(
                    downloadUrl,
                    job.LocalPath,
                    job.RemoteModified,
                    ct: ct);
                break;

            case SyncDirection.Upload:
                string remotePath = job.DownloadUrl ?? job.RelativePath;

                _ = await graphService.UploadFileAsync(accessToken, job.LocalPath, remotePath, parentFolderId: job.FolderId, ct: ct);

                Serilog.Log.Information("[Worker {Id}] Uploaded {Path}", workerId, job.RelativePath);
                break;

            case SyncDirection.Delete:
                if(File.Exists(job.LocalPath))
                    File.Delete(job.LocalPath);
                break;
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
