using System;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     Represents a Microsoft account registered with OneDrive Sync.
///
///     This is the <strong>sole</strong> entity that stores PII (display name, email,
///     Microsoft account ID).  Every other table references this account via the
///     synthetic <see cref="Id"/> <see cref="Guid"/> primary key — never via any
///     Microsoft identity detail.
/// </summary>
public sealed class Account
{
    /// <summary>Synthetic primary key — immutable once assigned.</summary>
    public Guid Id { get; init; }

    /// <summary>Human-readable display name (PII — Account table only).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Email address (PII — Account table only).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Microsoft identity token (PII — Account table only).</summary>
    public string MicrosoftAccountId { get; set; } = string.Empty;

    /// <summary>Auth state: Authenticated or AuthRequired (AU-05).</summary>
    public string AuthState { get; set; } = "Authenticated";

    /// <summary>Timestamp of user's consent decision for insecure token fallback storage, if given (AU-02, AU-03).</summary>
    public DateTimeOffset? ConsentDecisionMadeAt { get; set; }

    /// <summary>Absolute path to the local folder where this account's files are synced (AM-07).</summary>
    public string LocalSyncPath { get; set; } = string.Empty;

    /// <summary>Sync interval in minutes — Power User only (AM-05); default 15.</summary>
    public int SyncIntervalMinutes { get; set; } = 15;

    /// <summary>Max parallel transfer threads — Power User only (AM-05); range 1–10, default 5.</summary>
    public int ConcurrencyLimit { get; set; } = 5;

    /// <summary>Whether file metadata is written to the database after each sync (AM-12).</summary>
    public bool StoreFileMetadata { get; set; }

    /// <summary>UTC timestamp of the most recent completed sync run; null if never synced.</summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    /// <summary>True while a sync is actively running for this account (AM-08).</summary>
    public bool IsSyncActive { get; set; }
}
