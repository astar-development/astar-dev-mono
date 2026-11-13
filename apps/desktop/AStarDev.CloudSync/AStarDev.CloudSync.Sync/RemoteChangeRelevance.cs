using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts the remote changes whose old or new path the selection includes (#1398).</summary>
/// <param name="indexReader">Computes paths from the parent chain.</param>
/// <param name="evaluator">Decides whether a path is included.</param>
internal sealed class RemoteChangeRelevance(IRemoteIndexReader indexReader, ISyncSelectionEvaluator evaluator)
{
    private const char Separator = '/';

    internal async Task<RelevantChangeCounts> CountAsync(AccountId accountId, IReadOnlyList<RemoteChange> changes, IReadOnlyList<SelectionRule> rulesBefore, IReadOnlyList<SelectionRule> rulesAfter, CancellationToken cancellationToken)
    {
        var parentPaths = new Dictionary<string, Option<LocalRelativePath>>(StringComparer.Ordinal);
        var counts = RelevantChangeCountsFactory.None;

        foreach (var change in changes)
            counts = counts.Plus(await CountOneAsync(accountId, change, rulesBefore, rulesAfter, parentPaths, cancellationToken).ConfigureAwait(false));

        return counts;
    }

    private static RelevantChangeCounts Of(bool relevant, int added, int modified, int moved, int deleted) => relevant ? RelevantChangeCountsFactory.Create(added, modified, moved, deleted) : RelevantChangeCountsFactory.None;

    private static RelevantChangeCounts OfMove(bool wasIncluded, bool isIncluded) => (wasIncluded, isIncluded) switch
    {
        (true, true) => RelevantChangeCountsFactory.Create(0, 0, 1, 0),
        (true, false) => RelevantChangeCountsFactory.Create(0, 0, 0, 1),
        (false, true) => RelevantChangeCountsFactory.Create(1, 0, 0, 0),
        _ => RelevantChangeCountsFactory.None,
    };

    private static Option<LocalRelativePath> Join(Option<LocalRelativePath> parent, string name) =>
        LocalRelativePathFactory.Create(string.Join(Separator, new[] { parent.Match(path => path.Value ?? string.Empty, () => string.Empty), name }.Where(segment => segment.Length > 0))).Match(path => Option.Some(path), _ => Option.None<LocalRelativePath>());

    private async Task<RelevantChangeCounts> CountOneAsync(AccountId accountId, RemoteChange change, IReadOnlyList<SelectionRule> rulesBefore, IReadOnlyList<SelectionRule> rulesAfter, Dictionary<string, Option<LocalRelativePath>> parentPaths, CancellationToken cancellationToken)
    {
        switch (change)
        {
            case RemoteChange.Added added:
                return Of(IsIncluded(await PathOfAsync(accountId, added.Item.ParentId, added.Item.Name, parentPaths, cancellationToken).ConfigureAwait(false), rulesAfter), 1, 0, 0, 0);
            case RemoteChange.Modified modified:
                return Of(IsIncluded(await PathOfAsync(accountId, modified.Item.ParentId, modified.Item.Name, parentPaths, cancellationToken).ConfigureAwait(false), rulesAfter), 0, 1, 0, 0);
            case RemoteChange.Moved moved:
                var oldPath = await PathOfAsync(accountId, moved.OldParentId, moved.OldName, parentPaths, cancellationToken).ConfigureAwait(false);
                var newPath = await PathOfAsync(accountId, moved.Item.ParentId, moved.Item.Name, parentPaths, cancellationToken).ConfigureAwait(false);

                return OfMove(IsIncluded(oldPath, rulesBefore), IsIncluded(newPath, rulesAfter));
            case RemoteChange.Deleted deleted:
                return Of(IsIncluded((await indexReader.GetPathAsync(accountId, deleted.Id, cancellationToken).ConfigureAwait(false)).OrThrow(), rulesAfter), 0, 0, 0, 1);
            default:
                return RelevantChangeCountsFactory.None;
        }
    }

    private bool IsIncluded(Option<LocalRelativePath> path, IReadOnlyList<SelectionRule> rules) => path.Match(found => evaluator.IsIncluded(found, rules), () => false);

    private async Task<Option<LocalRelativePath>> PathOfAsync(AccountId accountId, Option<RemoteItemId> parentId, string name, Dictionary<string, Option<LocalRelativePath>> parentPaths, CancellationToken cancellationToken)
    {
        var parentPath = await parentId.Match(id => ParentPathAsync(accountId, id, parentPaths, cancellationToken), () => Task.FromResult(Option.None<LocalRelativePath>())).ConfigureAwait(false);

        return Join(parentPath, name);
    }

    private async Task<Option<LocalRelativePath>> ParentPathAsync(AccountId accountId, RemoteItemId parentId, Dictionary<string, Option<LocalRelativePath>> parentPaths, CancellationToken cancellationToken)
    {
        if (parentPaths.TryGetValue(parentId.Value, out var known))
            return known;

        var path = (await indexReader.GetPathAsync(accountId, parentId, cancellationToken).ConfigureAwait(false)).OrThrow();
        parentPaths[parentId.Value] = path;

        return path;
    }
}
