using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
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
    public async Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken)
    {
        var downloadJob = (DownloadSyncJob)job;
        var urlResult = await ResolveDownloadUrlAsync(downloadJob, accountId, tokenFactory, cancellationToken).ConfigureAwait(false);

        return await urlResult.MatchAsync(
            async url =>
            {
                var downloadResult = await downloader.DownloadAsync(url, downloadJob.Target.LocalPath, downloadJob.Metadata.RemoteModified).ConfigureAwait(false);

                return downloadResult.Match(
                    _ => (Result<SyncJob, string>)new Ok<SyncJob, string>(downloadJob),
                    error =>
                    {
                        OneDriveSyncClientMessages.DownloadFailed(logger, downloadJob.Target.RelativePath, error);

                        return new Fail<SyncJob, string>(error);
                    });
            },
            urlError =>
            {
                OneDriveSyncClientMessages.DownloadUrlResolveFailed(logger, downloadJob.Target.RelativePath, urlError);

                return new Fail<SyncJob, string>(urlError);
            }).ConfigureAwait(false);
    }

    private async Task<Result<string, string>> ResolveDownloadUrlAsync(DownloadSyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken)
    {
        if (job.DownloadUrl is Option<string>.Some downloadUrl)
            return new Ok<string, string>(downloadUrl.Value);

        OneDriveSyncClientMessages.DownloadUrlAbsent(logger, job.Target.RelativePath);

        return await graphService.GetDownloadUrlAsync(accountId, tokenFactory, job.Remote.RemoteItemId.Value, cancellationToken).ConfigureAwait(false);
    }
}
