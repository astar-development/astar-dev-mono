namespace AStarDev.CloudSync.Sync.Sqlite;

internal sealed class UploadSessionEntity
{
    public Guid AccountId { get; set; }

    public string PathKey { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public long ExpiresAtUtcTicks { get; set; }

    public long SourceLength { get; set; }

    public long SourceWriteUtcTicks { get; set; }

    public long ConfirmedBytes { get; set; }

    public string Target { get; set; } = string.Empty;
}
