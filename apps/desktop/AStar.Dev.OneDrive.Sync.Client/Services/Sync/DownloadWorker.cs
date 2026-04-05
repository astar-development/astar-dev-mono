using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

/// <summary>
/// A single download worker that drains jobs from a
/// <see cref="ChannelReader{T}"/> and executes them.
/// Multiple workers run concurrently — one per degree of parallelism.
/// </summary>
public sealed class DownloadWorker(int workerId, HttpDownloader downloader, IGraphService graphService, ISyncRepository syncRepository)
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
                Serilog.Log.Error(ex, "[Worker {Id}] EXCEPTION type={Type} message={Error} path={Path} stack={Stack}", workerId, ex.GetType().Name, ex.Message, job.LocalPath, ex.StackTrace);
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
                if(job.DownloadUrl is null)
                {
                    throw new InvalidOperationException($"No download URL for {job.RelativePath}");
                }

                await downloader.DownloadAsync(
                    job.DownloadUrl,
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
}
