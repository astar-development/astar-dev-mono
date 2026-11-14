using System.IO.Abstractions;
using System.Security.Cryptography;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using FileHashes = (string QuickXor, string Sha256, System.DateTimeOffset LastWrite);
using LinkCandidate = (AStarDev.CloudSync.Sync.RemoteItemSnapshot Item, AStarDev.CloudSync.Sync.RemoteItemKind.File File, AStarDev.CloudSync.Sync.ContentHash RemoteHash, long Size);

namespace AStarDev.CloudSync.Sync;

/// <summary>Decides whether a file both sides added is the same file (SE-06, #1534): the sizes and the cloud's QuickXorHash must match. A cloud item without a hash is never linked. A local file that cannot be read, or that changed since the scan, is a failure to retry on the next pass, never a mismatch. A link is the baseline row; the caller writes it. The file is read once for both the cloud hash and the SHA-256 the baseline keeps.</summary>
internal static class FirstSyncLinker
{
    private static readonly Result<Option<SyncBaselineEntry>, LocalOperationFailure> NotLinked = Result.Success<Option<SyncBaselineEntry>, LocalOperationFailure>(Option.None<SyncBaselineEntry>());

    internal static long BytesToHash(ConflictFinding finding, Option<RemoteItemSnapshot> remoteItem) => CandidateOf(finding, remoteItem).Match(candidate => candidate.Size, () => 0);

    internal static Task<Result<Option<SyncBaselineEntry>, LocalOperationFailure>> TryLinkAsync(AccountId accountId, IDirectoryInfo root, ConflictFinding finding, Option<RemoteItemSnapshot> remoteItem, FileProgress progress, CancellationToken cancellationToken) =>
        CandidateOf(finding, remoteItem).Match(candidate => LinkAsync(accountId, root, finding.Path, candidate, progress, cancellationToken), () => Task.FromResult(NotLinked));

    private static Option<LinkCandidate> CandidateOf(ConflictFinding finding, Option<RemoteItemSnapshot> remoteItem)
    {
        if (finding.Kind is not ConflictKind.BothAdded || finding.Local is not SideSnapshot.Present local)
            return Option.None<LinkCandidate>();

        return remoteItem.Match(item => item.Kind is RemoteItemKind.File file && file.Size == local.Size ? file.Hash.Map(hash => (item, file, hash, local.Size)) : Option.None<LinkCandidate>(), Option.None<LinkCandidate>);
    }

    private static LocalOperationFailure FailureOf(Exception exception) => exception switch
    {
        PathTooLongException => LocalOperationFailureFactory.CreatePathTooLong(),
        IOException => LocalOperationFailureFactory.CreateLocked(),
        UnauthorizedAccessException => LocalOperationFailureFactory.CreateAccessDenied(),
        _ => LocalOperationFailureFactory.CreateUnexpected(),
    };

    private static bool IsUnchanged(IFileInfo target, long size, DateTime lastWriteUtc)
    {
        target.Refresh();

        return target.Exists && target.Length == size && target.LastWriteTimeUtc == lastWriteUtc;
    }

    private static async Task<Result<FileHashes, LocalOperationFailure>> HashOnceAsync(IFileInfo target, long size, FileProgress progress, CancellationToken cancellationToken)
    {
        target.Refresh();
        if (!target.Exists || target.Length != size)
            return Result.Failure<FileHashes, LocalOperationFailure>(LocalOperationFailureFactory.CreateTargetChanged());

        var lastWriteUtc = target.LastWriteTimeUtc;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long read = 0;
        await using var stream = target.OpenRead();
        var quickXor = await QuickXorHash.ComputeAsync(stream, chunk => AppendAndReport(sha, chunk, ref read, size, progress), cancellationToken).ConfigureAwait(false);
        if (read != size || !IsUnchanged(target, size, lastWriteUtc))
            return Result.Failure<FileHashes, LocalOperationFailure>(LocalOperationFailureFactory.CreateTargetChanged());

        return Result.Success<FileHashes, LocalOperationFailure>((quickXor, Convert.ToHexStringLower(sha.GetHashAndReset()), new DateTimeOffset(lastWriteUtc, TimeSpan.Zero)));
    }

    private static void AppendAndReport(IncrementalHash sha, ReadOnlyMemory<byte> chunk, ref long read, long size, FileProgress progress)
    {
        sha.AppendData(chunk.Span);
        read += chunk.Length;
        progress.Report(new TransferProgress(read, size));
    }

    private static Result<Option<SyncBaselineEntry>, LocalOperationFailure> EntryFor(AccountId accountId, RelativeSyncPath path, LinkCandidate candidate, FileHashes hashes)
    {
        if (!string.Equals(hashes.QuickXor, candidate.RemoteHash.Value, StringComparison.Ordinal))
            return NotLinked;

        return ContentHashFactory.Create(hashes.Sha256).Match(
            hash => Result.Success<Option<SyncBaselineEntry>, LocalOperationFailure>(Option.Some(SyncBaselineEntryFactory.Create(accountId, path, candidate.Item.Id, SyncBaselineKindFactory.CreateFile(candidate.Size, hashes.LastWrite, hash, candidate.File.CTag, candidate.Item.ETag)))),
            _ => Result.Failure<Option<SyncBaselineEntry>, LocalOperationFailure>(LocalOperationFailureFactory.CreateUnexpected()));
    }

    private static async Task<Result<Option<SyncBaselineEntry>, LocalOperationFailure>> LinkAsync(AccountId accountId, IDirectoryInfo root, RelativeSyncPath path, LinkCandidate candidate, FileProgress progress, CancellationToken cancellationToken)
    {
        var target = root.FileSystem.FileInfo.New(root.FileSystem.Path.Join(root.FullName, path.Value));
        var hashed = await Try.RunAsync(() => HashOnceAsync(target, candidate.Size, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        progress.Finish();

        return hashed.Match(
            result => result.Match(hashes => EntryFor(accountId, path, candidate, hashes), Result.Failure<Option<SyncBaselineEntry>, LocalOperationFailure>),
            exception => Result.Failure<Option<SyncBaselineEntry>, LocalOperationFailure>(FailureOf(exception)));
    }
}
