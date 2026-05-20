using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class DownloadJobHandler(IHttpDownloader downloader, IGraphService graphService, ILogger<DownloadJobHandler> logger) : IJobHandler
{
    /// <inheritdoc />
    public bool CanHandle(SyncJob job) => job is DownloadSyncJob;

    /// <inheritdoc />
    public async Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, string accessToken, CancellationToken ct)
    {
        var downloadJob = (DownloadSyncJob)job;
        var urlResult = await ResolveDownloadUrlAsync(downloadJob, accountId, accessToken, ct).ConfigureAwait(false);

        return await urlResult.MatchAsync<Result<SyncJob, string>>(
            async url =>
            {
                var downloadResult = await downloader.DownloadAsync(url, downloadJob.Target.LocalPath, downloadJob.Metadata.RemoteModified, ct: ct).ConfigureAwait(false);

                return downloadResult.Match<Result<SyncJob, string>>(
                    _ => new Result<SyncJob, string>.Ok(downloadJob),
                    error =>
                    {
                        OneDriveSyncClientMessages.DownloadFailed(logger, downloadJob.Target.RelativePath, error);

                        return new Result<SyncJob, string>.Error(error);
                    });
            },
            urlError =>
            {
                OneDriveSyncClientMessages.DownloadUrlResolveFailed(logger, downloadJob.Target.RelativePath, urlError);

                return new Result<SyncJob, string>.Error(urlError);
            }).ConfigureAwait(false);
    }

    private async Task<Result<string, string>> ResolveDownloadUrlAsync(DownloadSyncJob job, string accountId, string accessToken, CancellationToken ct)
    {
        if (job.DownloadUrl is Option<string>.Some downloadUrl)
            return new Result<string, string>.Ok(downloadUrl.Value);

        OneDriveSyncClientMessages.DownloadUrlAbsent(logger, job.Target.RelativePath);

        return await graphService.GetDownloadUrlAsync(accountId, accessToken, job.Remote.RemoteItemId.Id, ct).ConfigureAwait(false);
    }
}
