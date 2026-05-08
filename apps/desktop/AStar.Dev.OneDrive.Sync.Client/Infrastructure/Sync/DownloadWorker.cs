using System.IO.Abstractions;
using System.Reactive;
using System.Threading.Channels;
using AStar.Dev.Functional.Extensions;
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

            await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.InProgress).ConfigureAwait(false);

            var currentJob = job;
            string? error = null;
            bool success = false;

            try
            {
                var jobResult = await ExecuteJobAsync(job, accessToken, ct).ConfigureAwait(false);

                if(jobResult is Result<SyncJob, string>.Ok completedJobResult)
                {
                    currentJob = completedJobResult.Value;
                    success = true;
                }
                else if(jobResult is Result<SyncJob, string>.Error jobErrorResult)
                {
                    error = jobErrorResult.Reason;
                }

                if(success)
                    await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Completed).ConfigureAwait(false);
                else
                    await syncRepository.UpdateJobStateAsync(job.Status.Id, SyncJobState.Failed, error).ConfigureAwait(false);
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

    private async Task<Result<SyncJob, string>> ExecuteJobAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        switch(job)
        {
            case DownloadSyncJob downloadJob:
                var urlResult = await ResolveDownloadUrlAsync(downloadJob, accessToken, ct).ConfigureAwait(false);

                if(urlResult is not Result<string, string>.Ok urlOk)
                {
                    var urlError = ((Result<string, string>.Error)urlResult).Reason;
                    Serilog.Log.Error("[Worker {Id}] Could not resolve download URL for {Path}: {Error}", workerId, downloadJob.Target.RelativePath, urlError);

                    return new Result<SyncJob, string>.Error(urlError);
                }

                var downloadResult = await downloader.DownloadAsync(urlOk.Value, downloadJob.Target.LocalPath, downloadJob.Metadata.RemoteModified, ct: ct).ConfigureAwait(false);

                if(downloadResult is not Result<Unit, string>.Ok)
                {
                    var downloadError = ((Result<Unit, string>.Error)downloadResult).Reason;
                    Serilog.Log.Error("[Worker {Id}] Download failed for {Path}: {Error}", workerId, downloadJob.Target.RelativePath, downloadError);

                    return new Result<SyncJob, string>.Error(downloadError);
                }

                return new Result<SyncJob, string>.Ok(downloadJob);

            case UploadSyncJob uploadJob:
                var uploadResult = await graphService.UploadFileAsync(accessToken, uploadJob.Target.LocalPath, uploadJob.Target.RelativePath, parentFolderId: uploadJob.Remote.FolderId.Id, ct: ct).ConfigureAwait(false);

                if(uploadResult is not Result<string, string>.Ok uploadOk)
                {
                    var uploadError = ((Result<string, string>.Error)uploadResult).Reason;
                    Serilog.Log.Error("[Worker {Id}] Upload failed for {Path}: {Error}", workerId, uploadJob.Target.RelativePath, uploadError);

                    return new Result<SyncJob, string>.Error(uploadError);
                }

                Serilog.Log.Information("[Worker {Id}] Uploaded {Path}", workerId, uploadJob.Target.RelativePath);

                return new Result<SyncJob, string>.Ok(uploadJob with { UploadedRemoteItemId = uploadOk.Value });

            case DeleteSyncJob deleteJob:
                if(fileSystem.File.Exists(deleteJob.Target.LocalPath))
                    fileSystem.File.Delete(deleteJob.Target.LocalPath);

                return new Result<SyncJob, string>.Ok(deleteJob);

            default:
                return new Result<SyncJob, string>.Ok(job);
        }
    }

    private async Task<Result<string, string>> ResolveDownloadUrlAsync(DownloadSyncJob job, string accessToken, CancellationToken ct)
    {
        if(job.DownloadUrl is not null)
            return new Result<string, string>.Ok(job.DownloadUrl);

        Serilog.Log.Debug("[Worker {Id}] DownloadUrl absent for {Path} — fetching on-demand", workerId, job.Target.RelativePath);

        return await graphService.GetDownloadUrlAsync(accessToken, job.Remote.RemoteItemId.Id, ct).ConfigureAwait(false);
    }
}
