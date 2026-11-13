using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public static class RemoteIndexData
{
    public const string Modified = "2026-11-01T10:00:00Z";

    public static RemoteItemId Id(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    public static ChangeCursor Cursor(string value) => ChangeCursorFactory.Create(value).Match(cursor => cursor, _ => throw new InvalidOperationException("The test cursor must be valid."));

    public static RemoteItemSnapshot Folder(string id, string parent, string name) =>
        RemoteItemSnapshotFactory.Create(Id(id), Option.Some(Id(parent)), name, RemoteItemKindFactory.CreateFolder(), DateTimeOffset.Parse(Modified, System.Globalization.CultureInfo.InvariantCulture), ETag($"e-{id}"));

    public static RemoteItemSnapshot File(string id, string parent, string name, string cTag = "c-1", string hash = "hash-1", string? eTag = null) =>
        RemoteItemSnapshotFactory.Create(Id(id), Option.Some(Id(parent)), name, RemoteItemKindFactory.CreateFile(10, HashOf(hash), Tag(cTag)), DateTimeOffset.Parse(Modified, System.Globalization.CultureInfo.InvariantCulture), ETag(eTag ?? $"e-{id}"));

    public static RemoteItemSnapshot Gone(string id, string parent) =>
        RemoteItemSnapshotFactory.Create(Id(id), Option.Some(Id(parent)), "gone", RemoteItemKindFactory.CreateDeleted(), DateTimeOffset.Parse(Modified, System.Globalization.CultureInfo.InvariantCulture), ETag($"e-{id}"));

    public static Option<ContentHash> HashOf(string value) => ContentHashFactory.Create(value).Match(hash => Option.Some(hash), _ => Option.None<ContentHash>());

    public static ContentTag Tag(string value) => ContentTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test tag must be valid."));

    public static EntityTag ETag(string value) => EntityTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test tag must be valid."));

    public static RemoteChangePage More(string cursor, params RemoteItemSnapshot[] items) => RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateMorePages(Cursor(cursor)));

    public static RemoteChangePage Caught(string cursor, params RemoteItemSnapshot[] items) => RemoteChangePageFactory.Create(items, PageContinuationFactory.CreateCaughtUp(Cursor(cursor)));
}
