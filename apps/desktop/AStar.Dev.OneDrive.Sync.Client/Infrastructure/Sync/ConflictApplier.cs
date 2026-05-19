using System.IO.Abstractions;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <inheritdoc />
public sealed class ConflictApplier(IHttpDownloader httpDownloader, IGraphService graphService, IFileSystem fileSystem, ILogger<ConflictApplier> logger) : IConflictApplier
{
    /// <inheritdoc />
    public async Task<bool> ApplyAsync(SyncConflict conflict, ConflictOutcome outcome, string accountId, string accessToken, CancellationToken ct)
    {
        switch (outcome)
        {
            case ConflictOutcome.UseRemote:
                return await ApplyUseRemoteAsync(conflict, accountId, accessToken, ct).ConfigureAwait(false);

            case ConflictOutcome.KeepBoth:
                ApplyKeepBoth(conflict);
                return true;

            default:
                return true;
        }
    }

    private async Task<bool> ApplyUseRemoteAsync(SyncConflict conflict, string accountId, string accessToken, CancellationToken ct)
    {
        var urlResult = await graphService.GetDownloadUrlAsync(accountId, accessToken, conflict.Remote.RemoteItemId.Id, ct).ConfigureAwait(false);

        return await urlResult.MatchAsync<bool>(
            async url =>
            {
                var downloadResult = await httpDownloader.DownloadAsync(url, conflict.Target.LocalPath, conflict.Snapshot.RemoteModified, ct: ct).ConfigureAwait(false);

                return downloadResult.Match<bool>(
                    _ => true,
                    downloadError =>
                    {
                        OneDriveSyncClientMessages.ConflictDownloadFailed(logger, conflict.Target.RelativePath, downloadError);

                        return false;
                    });
            },
            error =>
            {
                OneDriveSyncClientMessages.ConflictDownloadUrlFailed(logger, conflict.Target.RelativePath, error);

                return false;
            }).ConfigureAwait(false);
    }

    private void ApplyKeepBoth(SyncConflict conflict)
    {
        string keepBothName = ConflictResolver.MakeKeepBothName(conflict.Target.LocalPath, conflict.Snapshot.LocalModified, fileSystem);

        if (fileSystem.File.Exists(conflict.Target.LocalPath))
            fileSystem.File.Move(conflict.Target.LocalPath, keepBothName);
    }
}
