using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Finds baseline items that are no longer on disk (#1399). It reads the baseline in pages, so memory stays bounded, and ignores items the selection rules or the skip filter no longer cover, so removing a rule never reads as a deletion.</summary>
/// <param name="baselineReader">Reads the baseline.</param>
/// <param name="evaluator">Judges folders against the rules.</param>
internal sealed class LocalDeletionFinder(ISyncBaselineReader baselineReader, ISyncSelectionEvaluator evaluator)
{
    private const int PageSize = 5000;
    private const char Separator = '/';

    internal async Task<IReadOnlyList<LocalChange>> FindAsync(AccountId accountId, IDirectoryInfo root, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        var deleted = new List<LocalChange>();
        var after = Option.None<RelativeSyncPath>();
        while (true)
        {
            var page = (await baselineReader.GetUnderAsync(accountId, default, after, PageSize, cancellationToken).ConfigureAwait(false)).OrThrow();
            deleted.AddRange(page.Where(entry => IsTracked(entry, rules) && IsMissing(root, entry)).Select(entry => LocalChangeFactory.CreateDeleted(entry.Path, entry.ItemId)));
            cancellationToken.ThrowIfCancellationRequested();
            if (page.Count < PageSize)
                return deleted;

            after = Option.Some(page[^1].Path);
        }
    }

    private static bool IsMissing(IDirectoryInfo root, SyncBaselineEntry entry)
    {
        var full = root.FileSystem.Path.Join(root.FullName, entry.Path.Value);

        return entry.Kind is SyncBaselineKind.Folder ? !root.FileSystem.Directory.Exists(full) : !root.FileSystem.File.Exists(full);
    }

    private static string ParentOf(string path) => path.LastIndexOf(Separator) is var cut and >= 0 ? path[..cut] : string.Empty;

    private bool IsTracked(SyncBaselineEntry entry, IReadOnlyList<SelectionRule> rules)
    {
        var path = entry.Path.Value ?? string.Empty;
        var folder = entry.Kind is SyncBaselineKind.Folder ? path : ParentOf(path);

        return !SyncSkipFilter.IsSkipped(entry.Path) && LocalRelativePathFactory.Create(folder).Match(valid => evaluator.IsIncluded(valid, rules), _ => false);
    }
}
