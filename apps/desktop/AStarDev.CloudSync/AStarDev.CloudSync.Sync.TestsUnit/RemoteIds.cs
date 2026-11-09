using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public static class RemoteIds
{
    public static RemoteItemId Of(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    public static ChangeCursor Cursor(string value) => ChangeCursorFactory.Create(value).Match(cursor => cursor, _ => throw new InvalidOperationException("The test cursor must be valid."));

    public static ContentTag CTag(string value) => ContentTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test cTag must be valid."));

    public static EntityTag ETag(string value) => EntityTagFactory.Create(value).Match(tag => tag, _ => throw new InvalidOperationException("The test eTag must be valid."));

    public static ContentHash Hash(string value) => ContentHashFactory.Create(value).Match(hash => hash, _ => throw new InvalidOperationException("The test hash must be valid."));
}
