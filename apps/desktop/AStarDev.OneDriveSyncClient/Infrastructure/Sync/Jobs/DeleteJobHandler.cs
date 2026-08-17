using System.IO.Abstractions;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Infrastructure.Sync.Jobs;

/// <inheritdoc />
public sealed class DeleteJobHandler(IFileSystem fileSystem) : IJobHandler
{
    /// <inheritdoc />
    public bool CanHandle(SyncJob job) => job is DeleteSyncJob;

    /// <inheritdoc />
    public Task<Result<SyncJob, string>> HandleAsync(SyncJob job, string accountId, Func<CancellationToken, Task<string>> tokenFactory, CancellationToken cancellationToken)
    {
        var deleteJob = (DeleteSyncJob)job;

        if (fileSystem.File.Exists(deleteJob.Target.LocalPath))
            fileSystem.File.Delete(deleteJob.Target.LocalPath);

        return Task.FromResult<Result<SyncJob, string>>(new Ok<SyncJob, string>(deleteJob));
    }
}
