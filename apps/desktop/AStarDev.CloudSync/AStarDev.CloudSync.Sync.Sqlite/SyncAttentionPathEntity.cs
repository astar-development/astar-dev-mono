namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class SyncAttentionPathEntity
{
    public Guid AttentionId { get; set; }

    public string Path { get; set; } = string.Empty;
}
