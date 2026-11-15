namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class AccountLogLevelEntity
{
    public Guid AccountId { get; set; }

    public bool Verbose { get; set; }
}
