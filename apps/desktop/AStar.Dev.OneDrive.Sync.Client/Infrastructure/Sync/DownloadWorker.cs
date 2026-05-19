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
public sealed class DownloadWorker(int workerId, IHttpDownloader downloader, IGraphService graphService, ISyncRepository syncRepository, IFileSystem fileSystem) : IDownloadWorker
{
    /// <inheritdoc />
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
                (currentJob, success, error) = await ExecuteJobAsync(job, accessToken, ct)
                    .MatchAsync<SyncJob, string, (SyncJob, bool, string?)>(
                        completedJob => (completedJob, true, null),
                        reason => (currentJob, false, reason)).ConfigureAwait(false);

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

                return await urlResult.MatchAsync<Result<SyncJob, string>>(
                    async url =>
                    {
                        var downloadResult = await downloader.DownloadAsync(url, downloadJob.Target.LocalPath, downloadJob.Metadata.RemoteModified, ct: ct).ConfigureAwait(false);

                        return downloadResult.Match<Result<SyncJob, string>>(
                            _ => new Result<SyncJob, string>.Ok(downloadJob),
                            downloadError =>
                            {
                                Serilog.Log.Error("[Worker {Id}] Download failed for {Path}: {Error}", workerId, downloadJob.Target.RelativePath, downloadError);
                                return new Result<SyncJob, string>.Error(downloadError);
                            });
                    },
                    urlError =>
                    {
                        Serilog.Log.Error("[Worker {Id}] Could not resolve download URL for {Path}: {Error}", workerId, downloadJob.Target.RelativePath, urlError);
                        return new Result<SyncJob, string>.Error(urlError);
                    });

            case UploadSyncJob uploadJob:
                var uploadResult = await graphService.UploadFileAsync(accessToken, uploadJob.Target.LocalPath, uploadJob.Target.RelativePath, parentFolderId: uploadJob.Remote.FolderId.Id, ct: ct).ConfigureAwait(false);

                return uploadResult.Match<Result<SyncJob, string>>(
                    itemId =>
                    {
                        Serilog.Log.Information("[Worker {Id}] Uploaded {Path}", workerId, uploadJob.Target.RelativePath);
                        return new Result<SyncJob, string>.Ok(uploadJob with { UploadedRemoteItemId = itemId });
                    },
                    uploadError =>
                    {
                        Serilog.Log.Error("[Worker {Id}] Upload failed for {Path}: {Error}", workerId, uploadJob.Target.RelativePath, uploadError);
                        return new Result<SyncJob, string>.Error(uploadError);
                    });

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
