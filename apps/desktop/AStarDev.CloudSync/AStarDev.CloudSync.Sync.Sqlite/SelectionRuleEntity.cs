namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>The database row for a selection rule.</summary>
internal sealed class SelectionRuleEntity
{
    /// <summary>CloudSync's id for the account.</summary>
    public Guid AccountId { get; set; }

    /// <summary>The folder's path relative to the account's sync root; empty for the root itself.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The provider's id for the remote folder, when known.</summary>
    public string? RemoteId { get; set; }

    /// <summary>Whether the rule includes the folder; otherwise it excludes it.</summary>
    public bool IsInclude { get; set; }

    /// <summary>The direction of an include rule: TwoWay, UploadOnly or DownloadOnly.</summary>
    public string Direction { get; set; } = string.Empty;
}
