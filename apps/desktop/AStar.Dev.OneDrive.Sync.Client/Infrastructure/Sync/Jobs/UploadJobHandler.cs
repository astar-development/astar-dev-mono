using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
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
    public async Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken ct)
    {
        var uploadJob = (UploadSyncJob)job;
        var uploadResult = await graphService.UploadFileAsync(accountId, tokenFactory, uploadJob.Target.LocalPath, uploadJob.Target.RelativePath, parentFolderId: uploadJob.Remote.FolderId.Id, ct: ct).ConfigureAwait(false);

        return uploadResult.Match<Result<SyncJob, string>>(
            itemId =>
            {
                OneDriveSyncClientMessages.UploadCompleted(logger, uploadJob.Target.RelativePath);

                return new Result<SyncJob, string>.Ok(uploadJob with { UploadedRemoteItemId = itemId });
            },
            uploadError =>
            {
                OneDriveSyncClientMessages.UploadFailed(logger, uploadJob.Target.RelativePath, uploadError);

                return new Result<SyncJob, string>.Error(uploadError);
            });
    }
}
