namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class TransferConcurrencyEntity
{
    public Guid AccountId { get; set; }

    public int MaxConcurrentTransfers { get; set; }
}
