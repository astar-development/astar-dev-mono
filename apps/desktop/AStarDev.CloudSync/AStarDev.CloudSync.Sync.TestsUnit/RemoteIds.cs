using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public static class RemoteIds
{
    public static RemoteItemId Of(string value) => RemoteItemIdFactory.Create(value).Match(id => id, _ => throw new InvalidOperationException("The test id must be valid."));

    public static ChangeCursor Cursor(string value) => ChangeCursorFactory.Create(value).Match(cursor => cursor, _ => throw new InvalidOperationException("The test cursor must be valid."));
}
