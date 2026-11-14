using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides whether a file both sides added is the same file (SE-06, #1534): the sizes and the cloud's QuickXorHash must match. A cloud item without a hash is never linked. A link is the baseline row; the caller writes it.</summary>
/// <param name="hasher">Hashes the local file for the baseline row.</param>
internal sealed class FirstSyncLinker(IContentHasher hasher)
{
    internal async Task<Option<SyncBaselineEntry>> TryLinkAsync(AccountId accountId, IDirectoryInfo root, ConflictFinding finding, Option<RemoteItemSnapshot> remoteItem, CancellationToken cancellationToken)
    {
        if (finding.Kind is not ConflictKind.BothAdded || finding.Local is not SideSnapshot.Present local)
            return Option.None<SyncBaselineEntry>();

        return await remoteItem.Match(
            item => item.Kind is RemoteItemKind.File file && local.Size == file.Size ? file.Hash.Match(hash => LinkAsync(accountId, root, finding.Path, item, file, hash, cancellationToken), () => Task.FromResult(Option.None<SyncBaselineEntry>())) : Task.FromResult(Option.None<SyncBaselineEntry>()),
            () => Task.FromResult(Option.None<SyncBaselineEntry>())).ConfigureAwait(false);
    }

    private async Task<Option<SyncBaselineEntry>> LinkAsync(AccountId accountId, IDirectoryInfo root, RelativeSyncPath path, RemoteItemSnapshot item, RemoteItemKind.File file, ContentHash remoteHash, CancellationToken cancellationToken)
    {
        var target = root.FileSystem.FileInfo.New(root.FileSystem.Path.Join(root.FullName, path.Value));
        var quick = await Try.RunAsync(() => QuickXorOfAsync(target, file.Size, cancellationToken), cancellationToken).ConfigureAwait(false);
        var matches = quick.Match(found => string.Equals(found, remoteHash.Value, StringComparison.Ordinal), _ => false);
        if (!matches)
            return Option.None<SyncBaselineEntry>();

        return await hasher.HashAsync(target, cancellationToken)
            .MatchAsync(hash => Option.Some(SyncBaselineEntryFactory.Create(accountId, path, item.Id, SyncBaselineKindFactory.CreateFile(target.Length, new DateTimeOffset(target.LastWriteTimeUtc, TimeSpan.Zero), hash, file.CTag, item.ETag))), _ => Option.None<SyncBaselineEntry>())
            .ConfigureAwait(false);
    }

    private static async Task<string> QuickXorOfAsync(IFileInfo target, long expectedSize, CancellationToken cancellationToken)
    {
        target.Refresh();
        if (!target.Exists || target.Length != expectedSize)
            return string.Empty;

        await using var stream = target.OpenRead();

        return await QuickXorHash.ComputeAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
