using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ScriptedChangeFeed : IRemoteChangeFeed
{
    private static readonly DateTimeOffset Moment = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private readonly Queue<Result<RemoteChangePage, ProviderError>> pages = new();

    public List<Option<ChangeCursor>> Requested { get; } = [];

    public static RemoteItemSnapshot File(string id, string? parentId, string name) =>
        RemoteItemSnapshotFactory.Create(RemoteIds.Of(id), ParentOf(parentId), name, RemoteItemKindFactory.CreateFile(10, Option.None<ContentHash>(), RemoteIds.CTag($"c-{id}")), Moment, RemoteIds.ETag($"e-{id}"));

    public static RemoteItemSnapshot Folder(string id, string? parentId, string name) =>
        RemoteItemSnapshotFactory.Create(RemoteIds.Of(id), ParentOf(parentId), name, RemoteItemKindFactory.CreateFolder(), Moment, RemoteIds.ETag($"e-{id}"));

    public static RemoteItemSnapshot Deleted(string id, string? parentId, string name) =>
        RemoteItemSnapshotFactory.Create(RemoteIds.Of(id), ParentOf(parentId), name, RemoteItemKindFactory.CreateDeleted(), Moment, RemoteIds.ETag($"e-{id}"));

    public void Enqueue(Result<RemoteChangePage, ProviderError> page) => pages.Enqueue(page);

    public void EnqueueLast(params RemoteItemSnapshot[] items) => pages.Enqueue(Result.Success<RemoteChangePage, ProviderError>(RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateCaughtUp(RemoteIds.Cursor("delta-token")))));

    public void EnqueueMore(string next, params RemoteItemSnapshot[] items) => pages.Enqueue(Result.Success<RemoteChangePage, ProviderError>(RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateMorePages(RemoteIds.Cursor(next)))));

    public Task<Result<RemoteChangePage, ProviderError>> GetChangesAsync(AccountId accountId, Option<ChangeCursor> cursor, CancellationToken cancellationToken)
    {
        Requested.Add(cursor);

        return Task.FromResult(pages.Dequeue());
    }

    private static Option<RemoteItemId> ParentOf(string? parentId) => parentId is null ? Option.None<RemoteItemId>() : Option.Some(RemoteIds.Of(parentId));
}
