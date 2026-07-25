using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class UploadJobHandler(IGraphService graphService, ILogger<UploadJobHandler> logger) : IJobHandler
{
    /// <inheritdoc />
    public bool CanHandle(SyncJob job) => job is UploadSyncJob;

    /// <inheritdoc />
    public async Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken)
    {
        var uploadJob = (UploadSyncJob)job;
        var uploadResult = await graphService.UploadFileAsync(accountId, tokenFactory, uploadJob.Target.LocalPath, uploadJob.Target.RelativePath, parentFolderId: uploadJob.Remote.FolderId.Id, cancellationToken).ConfigureAwait(false);

        return uploadResult.Match(
            itemId =>
            {
                OneDriveSyncClientMessages.UploadCompleted(logger, uploadJob.Target.RelativePath);

                return (Result<SyncJob, string>)new Ok<SyncJob, string>(uploadJob with { UploadedRemoteItemId = itemId });
            },
            uploadError =>
            {
                OneDriveSyncClientMessages.UploadFailed(logger, uploadJob.Target.RelativePath, uploadError);

                return new Fail<SyncJob, string>(uploadError);
            });
    }
}
