using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads what an upload pass needs: the sync root, the rules, the quarantine, and the baseline and remote index items for the changed paths.</summary>
/// <param name="roots">Reads the account's sync root.</param>
/// <param name="rules">Reads the selection rules.</param>
/// <param name="quarantine">Tells which paths may not be synced now.</param>
/// <param name="baseline">Reads the baseline.</param>
/// <param name="remoteIndex">Reads the remote index.</param>
internal sealed class UploadPassInputs(ISyncRootReader roots, ISelectionRuleReader rules, IPathQuarantine quarantine, ISyncBaselineReader baseline, IRemoteIndexReader remoteIndex)
{
    private const char Separator = '/';
    private const int BaselinePageSize = 1000;

    internal Task<Result<UploadPassInput, UploadPassFailure>> LoadAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        Lifted(roots.GetAsync(accountId, cancellationToken))
            .BindAsync(root => RootDirectory(root).Match(directory => LoadWithRootAsync(accountId, directory, changes, cancellationToken), failure => Task.FromResult(Result.Failure<UploadPassInput, UploadPassFailure>(failure))));

    private static Unit Remember(Dictionary<RelativeSyncPath, SyncBaselineEntry> known, SyncBaselineEntry entry)
    {
        known[entry.Path] = entry;

        return Unit.Instance;
    }

    private static Task<Result<T, UploadPassFailure>> Lifted<T>(Task<Result<T, ErrorResponse>> read) => read.MatchAsync(Result.Success<T, UploadPassFailure>, _ => Result.Failure<T, UploadPassFailure>(UploadPassFailureFactory.CreateUnexpected()));

    private static Result<IDirectoryInfo, UploadPassFailure> RootDirectory(Option<SyncRoot> root) =>
        root.Match(
            found => found.Directory.Exists ? Result.Success<IDirectoryInfo, UploadPassFailure>(found.Directory) : Result.Failure<IDirectoryInfo, UploadPassFailure>(UploadPassFailureFactory.CreateRootUnavailable()),
            () => Result.Failure<IDirectoryInfo, UploadPassFailure>(UploadPassFailureFactory.CreateRootUnavailable()));

    private static IEnumerable<RelativeSyncPath> SelfAndAncestors(RelativeSyncPath path)
    {
        var value = path.Value ?? string.Empty;
        for (var end = value.Length; end > 0; end = value.LastIndexOf(Separator, end - 1))
            yield return new RelativeSyncPath(value[..end]);
    }

    private Task<Result<UploadPassInput, UploadPassFailure>> LoadWithRootAsync(AccountId accountId, IDirectoryInfo directory, LocalChangeSet changes, CancellationToken cancellationToken) =>
        Lifted(quarantine.ReadAsync(accountId, cancellationToken))
            .BindAsync(snapshot => Lifted(rules.GetAsync(accountId, cancellationToken))
                .BindAsync(ruleList => ReadIndexesAsync(accountId, directory, Allowed(changes, snapshot), ruleList, cancellationToken)));

    private static LocalChangeSet Allowed(LocalChangeSet changes, QuarantineSnapshot snapshot) =>
        LocalChangeSetFactory.Create(changes.Changes.Where(change => snapshot.StateOf(change.Path).AllowsSyncAction()), changes.Holds, changes.Summary.Touched, changes.Summary.Hashed);

    private Task<Result<UploadPassInput, UploadPassFailure>> ReadIndexesAsync(AccountId accountId, IDirectoryInfo directory, LocalChangeSet allowed, IReadOnlyList<SelectionRule> ruleList, CancellationToken cancellationToken) =>
        Try.RunAsync(() => ReadAsync(accountId, directory, allowed, ruleList, cancellationToken), cancellationToken)
            .MatchAsync(Result.Success<UploadPassInput, UploadPassFailure>, _ => Result.Failure<UploadPassInput, UploadPassFailure>(UploadPassFailureFactory.CreateUnexpected()));

    private async Task<UploadPassInput> ReadAsync(AccountId accountId, IDirectoryInfo directory, LocalChangeSet allowed, IReadOnlyList<SelectionRule> ruleList, CancellationToken cancellationToken)
    {
        var paths = allowed.Changes.Select(change => change.Path).ToList();
        var known = new Dictionary<RelativeSyncPath, SyncBaselineEntry>();
        foreach (var path in paths.SelectMany(SelfAndAncestors).Distinct())
            _ = (await baseline.GetAsync(accountId, path, cancellationToken).ConfigureAwait(false)).OrThrow().Match(entry => Remember(known, entry), () => Unit.Instance);

        var deletedFolders = allowed.Changes.OfType<LocalChange.Deleted>().Where(deleted => known.TryGetValue(deleted.Path, out var entry) && entry.Kind is SyncBaselineKind.Folder).Select(deleted => deleted.Path).ToList();
        foreach (var folder in deletedFolders)
            await ReadBelowAsync(accountId, folder, known, cancellationToken).ConfigureAwait(false);

        var remote = await ReadRemoteAsync(accountId, paths, deletedFolders, cancellationToken).ConfigureAwait(false);

        return UploadPassInputFactory.Create(directory, allowed, ruleList, known, remote);
    }

    private async Task ReadBelowAsync(AccountId accountId, RelativeSyncPath folder, Dictionary<RelativeSyncPath, SyncBaselineEntry> known, CancellationToken cancellationToken)
    {
        var after = Option.None<RelativeSyncPath>();
        while (true)
        {
            var page = (await baseline.GetUnderAsync(accountId, folder, after, BaselinePageSize, cancellationToken).ConfigureAwait(false)).OrThrow();
            foreach (var entry in page)
                known[entry.Path] = entry;

            if (page.Count < BaselinePageSize)
                return;

            after = Option.Some(page[^1].Path);
        }
    }

    private async Task<Dictionary<RelativeSyncPath, RemoteItemSnapshot>> ReadRemoteAsync(AccountId accountId, IEnumerable<RelativeSyncPath> paths, IEnumerable<RelativeSyncPath> deletedFolders, CancellationToken cancellationToken)
    {
        var found = new Dictionary<RelativeSyncPath, RemoteItemSnapshot>();
        var listings = new Dictionary<string, IReadOnlyList<RemoteItemSnapshot>>(StringComparer.Ordinal);
        foreach (var path in paths.Concat(deletedFolders))
            await WalkAsync(accountId, path, found, listings, cancellationToken).ConfigureAwait(false);

        foreach (var folder in deletedFolders.Where(found.ContainsKey))
            await ExpandAsync(accountId, folder, found[folder], found, listings, cancellationToken).ConfigureAwait(false);

        return found;
    }

    private async Task WalkAsync(AccountId accountId, RelativeSyncPath path, Dictionary<RelativeSyncPath, RemoteItemSnapshot> found, Dictionary<string, IReadOnlyList<RemoteItemSnapshot>> listings, CancellationToken cancellationToken)
    {
        var parent = Option.None<RemoteItemId>();
        var prefix = string.Empty;
        foreach (var segment in (path.Value ?? string.Empty).Split(Separator))
        {
            prefix = prefix.Length == 0 ? segment : prefix + Separator + segment;
            var children = await ChildrenAsync(accountId, parent, listings, cancellationToken).ConfigureAwait(false);
            var match = children.FirstOrNone(child => string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase));
            if (!match.Match(item => { found[new RelativeSyncPath(prefix)] = item; parent = Option.Some(item.Id); return true; }, () => false))
                return;
        }
    }

    private async Task ExpandAsync(AccountId accountId, RelativeSyncPath folder, RemoteItemSnapshot snapshot, Dictionary<RelativeSyncPath, RemoteItemSnapshot> found, Dictionary<string, IReadOnlyList<RemoteItemSnapshot>> listings, CancellationToken cancellationToken)
    {
        foreach (var child in await ChildrenAsync(accountId, Option.Some(snapshot.Id), listings, cancellationToken).ConfigureAwait(false))
        {
            var childPath = new RelativeSyncPath(folder.Value + Separator + child.Name);
            found[childPath] = child;
            if (child.Kind is RemoteItemKind.Folder)
                await ExpandAsync(accountId, childPath, child, found, listings, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<RemoteItemSnapshot>> ChildrenAsync(AccountId accountId, Option<RemoteItemId> parent, Dictionary<string, IReadOnlyList<RemoteItemSnapshot>> listings, CancellationToken cancellationToken)
    {
        var key = parent.Match(id => id.Value, () => string.Empty);
        if (listings.TryGetValue(key, out var cached))
            return cached;

        var listed = (await remoteIndex.GetChildrenAsync(accountId, parent, cancellationToken).ConfigureAwait(false)).OrThrow();
        listings[key] = listed;

        return listed;
    }
}
