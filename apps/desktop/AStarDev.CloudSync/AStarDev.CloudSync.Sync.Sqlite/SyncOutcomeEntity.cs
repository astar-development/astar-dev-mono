namespace AStarDev.CloudSync.Sync.Sqlite;

/// <summary>The status an account's last sync run left: one row per account.</summary>
internal sealed class SyncOutcomeEntity
{
    /// <summary>The account.</summary>
    public Guid AccountId { get; set; }

    /// <summary>The status case, and for a failure its reason.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Conflicts waiting for the user, for a needs-attention status.</summary>
    public int Conflicts { get; set; }

    /// <summary>Held deletions waiting for the user, for a needs-attention status.</summary>
    public int HeldDeletions { get; set; }

    /// <summary>When the status was saved.</summary>
    public DateTimeOffset SavedAt { get; set; }
}
