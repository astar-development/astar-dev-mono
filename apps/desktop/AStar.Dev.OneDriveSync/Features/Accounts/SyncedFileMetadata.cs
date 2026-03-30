namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     Stores metadata for a file in an account's synced folders when the per-account
///     "Store file metadata in database" flag (AM-12) is enabled.
///
///     One row per file per account. Rows are retained when AM-12 is disabled (DB-06).
///     Cascade-deleted when the owning <see cref="Account" /> is removed (AM-13, AM-15).
/// </summary>
public sealed class SyncedFileMetadata
{
    /// <summary>Auto-increment primary key.</summary>
    public long Id { get; init; }

    /// <summary>Synthetic account FK — no PII; references <see cref="Account.Id" />.</summary>
    public Guid AccountId { get; init; }

    /// <summary>OneDrive item ID for the remote file.</summary>
    public string RemoteItemId { get; set; } = string.Empty;

    /// <summary>Path relative to the account's local sync root.</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>File name (without directory component).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>SHA-256 checksum of the file content (hex-encoded).</summary>
    public string Sha256Checksum { get; set; } = string.Empty;

    /// <summary>Last-modified timestamp; stored as Unix milliseconds in the database via value converter (DB-01).</summary>
    public DateTimeOffset LastModifiedUtc { get; set; }

    /// <summary>Created timestamp; stored as Unix milliseconds in the database via value converter (DB-01).</summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>Navigation property to the owning account.</summary>
    public Account Account { get; init; } = null!;
}
