using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Counts the files of a cloud account by reading its whole change feed from the start (#1801). Nothing is saved: the feed is read without a stored cursor and the cursor it ends on is dropped, so the audit cannot disturb the incremental sync. Items are held in memory until the feed ends, because a child can arrive before its parent folder.</summary>
/// <param name="evaluator">Judges folders against the selection rules, as the sync does.</param>
/// <param name="logger">Reports a repeated link only; never a name.</param>
internal sealed class CloudFileCounter(ISyncSelectionEvaluator evaluator, ILogger<CloudFileCounter> logger)
{
    private const string Location = nameof(CloudFileCounter);
    private const char Separator = '/';

    internal async Task<Result<long, ProviderError>> CountAsync(IRemoteChangeFeed feed, AccountId accountId, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        var items = new Dictionary<string, RemoteItemSnapshot>(StringComparer.Ordinal);

        return (await ReadAsync(feed, accountId, Option.None<ChangeCursor>(), items, new HashSet<string>(StringComparer.Ordinal), cancellationToken).ConfigureAwait(false))
            .Match(read => Result.Success<long, ProviderError>(CountIncluded(read, rules)), error => Result.Failure<long, ProviderError>(error));
    }

    private static Option<string> FolderPathOf(Option<RemoteItemId> folderId, Dictionary<string, RemoteItemSnapshot> items, Dictionary<string, Option<string>> known, HashSet<string> walking) =>
        folderId.Match(id => ResolveFolder(id, items, known, walking), () => Option.Some(string.Empty));

    private static Option<string> ResolveFolder(RemoteItemId id, Dictionary<string, RemoteItemSnapshot> items, Dictionary<string, Option<string>> known, HashSet<string> walking)
    {
        if (known.TryGetValue(id.Value, out var remembered))
            return remembered;

        if (!items.TryGetValue(id.Value, out var folder))
            return Option.Some(string.Empty);

        if (folder.Kind is not RemoteItemKind.Folder || !walking.Add(id.Value))
            return Option.None<string>();

        var path = FolderPathOf(folder.ParentId, items, known, walking).Map(parent => parent.Length == 0 ? folder.Name : $"{parent}{Separator}{folder.Name}");
        _ = walking.Remove(id.Value);
        known[id.Value] = path;

        return path;
    }

    private static bool IsFile(RemoteItemSnapshot item) => item.Kind is RemoteItemKind.File;

    private long CountIncluded(Dictionary<string, RemoteItemSnapshot> items, IReadOnlyList<SelectionRule> rules)
    {
        var known = new Dictionary<string, Option<string>>(StringComparer.Ordinal);
        var included = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var walking = new HashSet<string>(StringComparer.Ordinal);

        return items.Values.Where(IsFile).LongCount(file => FolderPathOf(file.ParentId, items, known, walking).Match(folder => IsCounted(folder, file.Name, rules, included), () => false));
    }

    private bool IsCounted(string folder, string name, IReadOnlyList<SelectionRule> rules, Dictionary<string, bool> included)
    {
        if (!included.TryGetValue(folder, out var folderIncluded))
        {
            folderIncluded = LocalRelativePathFactory.Create(folder).Match(path => evaluator.IsIncluded(path, rules), _ => false);
            included[folder] = folderIncluded;
        }

        var full = folder.Length == 0 ? name : $"{folder}{Separator}{name}";

        return folderIncluded && RelativeSyncPathFactory.Create(full).Match(path => !SyncSkipFilter.IsSkipped(path), _ => false);
    }

    private Task<Result<Dictionary<string, RemoteItemSnapshot>, ProviderError>> Continue(IRemoteChangeFeed feed, AccountId accountId, RemoteChangePage page, Dictionary<string, RemoteItemSnapshot> items, HashSet<string> seen, CancellationToken cancellationToken)
    {
        foreach (var item in page.Items)
            items[item.Id.Value] = item;

        switch (page.Next)
        {
            case PageContinuation.MorePages more when seen.Add(more.Next.Value):
                return ReadAsync(feed, accountId, Option.Some(more.Next), items, seen, cancellationToken);
            case PageContinuation.MorePages:
                LogMessage.Warning(logger, Location, "The change feed returned a link it had already returned, so the audit stopped.");

                return Task.FromResult(Result.Failure<Dictionary<string, RemoteItemSnapshot>, ProviderError>(ProviderErrorFactory.CreateUnexpected()));
            default:
                return Task.FromResult(Result.Success<Dictionary<string, RemoteItemSnapshot>, ProviderError>(items));
        }
    }

    private async Task<Result<Dictionary<string, RemoteItemSnapshot>, ProviderError>> ReadAsync(IRemoteChangeFeed feed, AccountId accountId, Option<ChangeCursor> cursor, Dictionary<string, RemoteItemSnapshot> items, HashSet<string> seen, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await feed.GetChangesAsync(accountId, cursor, cancellationToken)
            .MatchAsync(page => Continue(feed, accountId, page, items, seen, cancellationToken), error => Task.FromResult(Result.Failure<Dictionary<string, RemoteItemSnapshot>, ProviderError>(error)))
            .ConfigureAwait(false);
    }
}
