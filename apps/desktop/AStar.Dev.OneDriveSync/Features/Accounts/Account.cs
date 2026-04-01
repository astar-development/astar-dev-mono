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
}
