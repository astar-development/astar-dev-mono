namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class RemoteCursorEntity
{
    public Guid AccountId { get; set; }

    public string? Cursor { get; set; }

    public Guid? ResyncPass { get; set; }
}
