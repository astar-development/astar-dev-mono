using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds baseline items that are no longer on disk (#1399). A name matches ignoring case, because the baseline keeps the remote casing and a case-sensitive file system may keep another; a stat or listing error reads as present, never as deleted. It reads the baseline in pages, so memory stays bounded, and ignores items the selection rules or the skip filter no longer cover, so removing a rule never reads as a deletion.</summary>
/// <param name="baselineReader">Reads the baseline.</param>
/// <param name="evaluator">Judges folders against the rules.</param>
internal sealed class LocalDeletionFinder(ISyncBaselineReader baselineReader, ISyncSelectionEvaluator evaluator)
{
    private const int PageSize = 5000;
    private const char Separator = '/';

    internal async Task<DeletionScan> FindAsync(AccountId accountId, IDirectoryInfo root, IReadOnlyList<SelectionRule> rules, IReadOnlySet<string> blocked, CancellationToken cancellationToken)
    {
        var pending = new List<PendingDeletion>();
        var unreadable = new List<LocalChange>();
        var totals = new Dictionary<SyncPairId, int>();
        var after = Option.None<RelativeSyncPath>();
        while (true)
        {
            var page = (await baselineReader.GetUnderAsync(accountId, default, after, PageSize, cancellationToken).ConfigureAwait(false)).OrThrow();
            foreach (var entry in page)
                Classify(entry, root, rules, blocked, pending, unreadable, totals);

            cancellationToken.ThrowIfCancellationRequested();
            if (page.Count < PageSize)
                return new DeletionScan(unreadable, pending, totals);

            after = Option.Some(page[^1].Path);
        }
    }

    private void Classify(SyncBaselineEntry entry, IDirectoryInfo root, IReadOnlyList<SelectionRule> rules, IReadOnlySet<string> blocked, List<PendingDeletion> pending, List<LocalChange> unreadable, Dictionary<SyncPairId, int> totals)
    {
        var path = entry.Path.Value ?? string.Empty;
        var folder = entry.Kind is SyncBaselineKind.Folder ? path : ParentOf(path);
        if (SyncSkipFilter.IsSkipped(entry.Path) || !LocalRelativePathFactory.Create(folder).Match(valid => evaluator.IsIncluded(valid, rules), _ => false))
            return;

        var pair = PairOf(entry.AccountId, folder, rules);
        totals[pair] = totals.GetValueOrDefault(pair) + 1;
        if (IsBlocked(path, blocked))
            unreadable.Add(LocalChangeFactory.CreateUnreadable(entry.Path));
        else if (IsMissing(root, entry))
            pending.Add(new PendingDeletion(entry.Path, entry.ItemId, pair));
    }

    private static bool IsBlocked(string path, IReadOnlySet<string> blocked) => blocked.Count > 0 && Prefixes(path).Any(blocked.Contains);

    private static IEnumerable<string> Prefixes(string path)
    {
        for (var cut = path.IndexOf(Separator); cut >= 0; cut = path.IndexOf(Separator, cut + 1))
            yield return path[..cut];

        yield return path;
    }

    private static SyncPairId PairOf(AccountId accountId, string folder, IReadOnlyList<SelectionRule> rules)
    {
        var path = LocalRelativePathFactory.Create(folder).Match(valid => valid, _ => default);
        var topMost = rules.Where(rule => rule.Kind is SelectionRuleKind.Include && SelectionPaths.IsAncestorOrSelf(rule.Path, path)).MinBy(rule => SelectionPaths.Depth(rule.Path));

        return SyncPairIdFactory.CreateForRule(accountId, topMost?.Path ?? default);
    }

    private static bool IsMissing(IDirectoryInfo root, SyncBaselineEntry entry) => !Try.Run(() => IsPresent(root.FileSystem, root.FullName, (entry.Path.Value ?? string.Empty).Split(Separator), entry.Kind is SyncBaselineKind.Folder)).Match(present => present, _ => true);

    private static bool IsPresent(IFileSystem fileSystem, string current, string[] segments, bool isFolder)
    {
        foreach (var segment in segments)
        {
            var exact = fileSystem.Path.Join(current, segment);
            if (fileSystem.Directory.Exists(exact) || fileSystem.File.Exists(exact))
            {
                current = exact;

                continue;
            }

            var match = fileSystem.Directory.Exists(current) ? fileSystem.Directory.EnumerateFileSystemEntries(current).Select(fileSystem.Path.GetFileName).FirstOrDefault(name => string.Equals(name, segment, StringComparison.OrdinalIgnoreCase)) : null;
            if (match is null)
                return false;

            current = fileSystem.Path.Join(current, match);
        }

        return isFolder ? fileSystem.Directory.Exists(current) : fileSystem.File.Exists(current);
    }

    private static string ParentOf(string path) => path.LastIndexOf(Separator) is var cut and >= 0 ? path[..cut] : string.Empty;
}
