using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class UploadJobHandler(IGraphService graphService) : IJobHandler
{
    /// <inheritdoc />
    public bool CanHandle(SyncJob job) => job is UploadSyncJob;

    /// <inheritdoc />
    public async Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accessToken, CancellationToken ct)
    {
        var uploadJob = (UploadSyncJob)job;
        var uploadResult = await graphService.UploadFileAsync(accessToken, uploadJob.Target.LocalPath, uploadJob.Target.RelativePath, parentFolderId: uploadJob.Remote.FolderId.Id, ct: ct).ConfigureAwait(false);

        return uploadResult.Match<Result<SyncJob, string>>(
            itemId =>
            {
                Serilog.Log.Information("Uploaded {Path}", uploadJob.Target.RelativePath);

                return new Result<SyncJob, string>.Ok(uploadJob with { UploadedRemoteItemId = itemId });
            },
            uploadError =>
            {
                Serilog.Log.Error("Upload failed for {Path}: {Error}", uploadJob.Target.RelativePath, uploadError);

                return new Result<SyncJob, string>.Error(uploadError);
            });
    }
}
