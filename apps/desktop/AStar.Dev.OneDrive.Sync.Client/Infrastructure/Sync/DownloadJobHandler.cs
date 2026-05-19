using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class DownloadJobHandler(IHttpDownloader downloader, IGraphService graphService) : IJobHandler
{
    /// <inheritdoc />
    public bool CanHandle(SyncJob job) => job is DownloadSyncJob;

    /// <inheritdoc />
    public async Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        var downloadJob = (DownloadSyncJob)job;
        var urlResult = await ResolveDownloadUrlAsync(downloadJob, accessToken, ct).ConfigureAwait(false);

        return await urlResult.MatchAsync<Result<SyncJob, string>>(
            async url =>
            {
                var downloadResult = await downloader.DownloadAsync(url, downloadJob.Target.LocalPath, downloadJob.Metadata.RemoteModified, ct: ct).ConfigureAwait(false);

                return downloadResult.Match<Result<SyncJob, string>>(
                    _ => new Result<SyncJob, string>.Ok(downloadJob),
                    error =>
                    {
                        Serilog.Log.Error("Download failed for {Path}: {Error}", downloadJob.Target.RelativePath, error);

                        return new Result<SyncJob, string>.Error(error);
                    });
            },
            urlError =>
            {
                Serilog.Log.Error("Could not resolve download URL for {Path}: {Error}", downloadJob.Target.RelativePath, urlError);

                return new Result<SyncJob, string>.Error(urlError);
            }).ConfigureAwait(false);
    }

    private async Task<Result<string, string>> ResolveDownloadUrlAsync(DownloadSyncJob job, string accessToken, CancellationToken ct)
    {
        if(job.DownloadUrl is not null)
            return new Result<string, string>.Ok(job.DownloadUrl);

        Serilog.Log.Debug("DownloadUrl absent for {Path} — fetching on-demand", job.Target.RelativePath);

        return await graphService.GetDownloadUrlAsync(accessToken, job.Remote.RemoteItemId.Id, ct).ConfigureAwait(false);
    }
}
