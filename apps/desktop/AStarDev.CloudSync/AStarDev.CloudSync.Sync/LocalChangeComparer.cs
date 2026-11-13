using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Compares one scanned item with its baseline row (#1399). Times are compared to the stored local write time exactly, never to the remote time. A file is hashed only when its size or write time differs.</summary>
/// <param name="baselineReader">Reads the baseline row of a path.</param>
/// <param name="hasher">Hashes a changed file.</param>
/// <param name="time">The clock that decides whether a file may still be written.</param>
internal sealed class LocalChangeComparer(ISyncBaselineReader baselineReader, IContentHasher hasher, TimeProvider time)
{
    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(2);

    internal async Task<LocalComparison> CompareAsync(AccountId accountId, LocalWalkItem item, CancellationToken cancellationToken)
    {
        var baseline = (await baselineReader.GetAsync(accountId, item.Path, cancellationToken).ConfigureAwait(false)).OrThrow();

        return await item.Info.Match(
            info => info is IFileInfo file ? CompareFileAsync(accountId, item.Path, file, baseline, cancellationToken) : Task.FromResult(CompareFolder(item.Path, info, baseline)),
            () => Task.FromResult(LocalComparison.Changed(LocalChangeFactory.CreateUnreadable(item.Path), hashed: false))).ConfigureAwait(false);
    }

    private static LocalComparison CompareFolder(RelativeSyncPath path, IFileSystemInfo folder, Option<SyncBaselineEntry> baseline)
    {
        if (baseline.Match(entry => entry.Kind is SyncBaselineKind.Folder, () => false))
            return LocalComparison.Unchanged;

        if (baseline.Match(_ => true, () => false))
            return LocalComparison.Changed(LocalChangeFactory.CreateUnreadable(path), hashed: false);

        return Observe(folder).Match(seen => LocalComparison.Changed(LocalChangeFactory.CreateAdded(path, LocalItemKindFactory.CreateFolder(), 0, seen.WrittenAt), hashed: false), () => LocalComparison.Unchanged);
    }

    private static Option<(long Size, DateTimeOffset WrittenAt)> Observe(IFileSystemInfo info) =>
        Try.Run(() => (Size: info is IFileInfo file ? file.Length : 0L, WrittenAt: new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero))).Match(seen => Option.Some(seen), _ => Option.None<(long, DateTimeOffset)>());

    private async Task<LocalComparison> CompareFileAsync(AccountId accountId, RelativeSyncPath path, IFileInfo file, Option<SyncBaselineEntry> baseline, CancellationToken cancellationToken)
    {
        file.Refresh();
        var observed = Observe(file);

        return await observed.Match(
            seen => baseline.Match(entry => CompareWithBaselineAsync(accountId, path, file, seen, entry, cancellationToken), () => Task.FromResult(IsSettled(seen.WrittenAt) ? LocalComparison.Changed(LocalChangeFactory.CreateAdded(path, LocalItemKindFactory.CreateFile(), seen.Size, seen.WrittenAt), hashed: false) : Deferred(path))),
            () => Task.FromResult(LocalComparison.Unchanged)).ConfigureAwait(false);
    }

    private async Task<LocalComparison> CompareWithBaselineAsync(AccountId accountId, RelativeSyncPath path, IFileInfo file, (long Size, DateTimeOffset WrittenAt) seen, SyncBaselineEntry entry, CancellationToken cancellationToken)
    {
        if (entry.Kind is not SyncBaselineKind.File known)
            return LocalComparison.Changed(LocalChangeFactory.CreateUnreadable(path), hashed: false);

        if (known.Size == seen.Size && known.LastWriteUtc == seen.WrittenAt)
            return LocalComparison.Unchanged;

        if (!IsSettled(seen.WrittenAt))
            return Deferred(path);

        var hashed = await hasher.HashAsync(file, cancellationToken).ConfigureAwait(false);

        return hashed.Match(hash => Decide(entry, known, seen, hash), _ => LocalComparison.Changed(LocalChangeFactory.CreateUnreadable(path), hashed: true));
    }

    private static LocalComparison Decide(SyncBaselineEntry entry, SyncBaselineKind.File known, (long Size, DateTimeOffset WrittenAt) seen, ContentHash hash) => hash.Equals(known.Hash)
        ? LocalComparison.TouchedOnly(SyncBaselineEntryFactory.Create(entry.AccountId, entry.Path, entry.ItemId, SyncBaselineKindFactory.CreateFile(seen.Size, seen.WrittenAt, known.Hash, known.CTag, known.ETag)))
        : LocalComparison.Changed(LocalChangeFactory.CreateModified(entry.Path, seen.Size, seen.WrittenAt, hash), hashed: true);

    private static LocalComparison Deferred(RelativeSyncPath path) => LocalComparison.Changed(LocalChangeFactory.CreateDeferred(path), hashed: false);

    private bool IsSettled(DateTimeOffset writtenAt)
    {
        var age = time.GetUtcNow() - writtenAt;

        return age.Duration() >= SettleTime;
    }
}
