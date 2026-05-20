using System.IO.Abstractions;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class DeleteJobHandler(IFileSystem fileSystem) : IJobHandler
{
    /// <inheritdoc />
    public bool CanHandle(SyncJob job) => job is DeleteSyncJob;

    /// <inheritdoc />
    public Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, string accessToken, CancellationToken ct)
    {
        var deleteJob = (DeleteSyncJob)job;

        if(fileSystem.File.Exists(deleteJob.Target.LocalPath))
            fileSystem.File.Delete(deleteJob.Target.LocalPath);

        return Task.FromResult<Result<SyncJob, string>>(new Result<SyncJob, string>.Ok(deleteJob));
    }
}
