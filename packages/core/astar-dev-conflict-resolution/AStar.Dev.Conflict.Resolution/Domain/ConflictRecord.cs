using System;

namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>
///     Represents a single detected sync conflict that is persisted to the queue until resolved.
///     Entities with state use <c>class</c> per repo conventions.
/// </summary>
public sealed class ConflictRecord
{
    /// <summary>Synthetic primary key — immutable once assigned.</summary>
    public Guid Id { get; init; }

    /// <summary>The account (by synthetic Guid) that owns this conflict.</summary>
    public Guid AccountId { get; init; }

    /// <summary>The file path of the conflicted item.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>The display name of the account (for UI only — read from Accounts table at query time).</summary>
    public string AccountDisplayName { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the local file was last modified.</summary>
    public DateTimeOffset LocalLastModified { get; init; }

    /// <summary>UTC timestamp when the remote file was last modified.</summary>
    public DateTimeOffset RemoteLastModified { get; init; }

    /// <summary>Nature of the detected conflict.</summary>
    public ConflictType ConflictType { get; init; }

    /// <summary>UTC timestamp when the conflict was detected.</summary>
    public DateTimeOffset DetectedAt { get; init; }

    /// <summary>Whether the conflict has been resolved (resolved conflicts remain for audit).</summary>
    public bool IsResolved { get; set; }

    /// <summary>The strategy applied when resolving; null while pending.</summary>
    public ResolutionStrategy? AppliedStrategy { get; set; }
}

/// <summary>Factory for <see cref="ConflictRecord"/>.</summary>
public static class ConflictRecordFactory
{
    /// <summary>Creates a new pending <see cref="ConflictRecord"/>.</summary>
    public static ConflictRecord Create(Guid accountId, string filePath, DateTimeOffset localLastModified, DateTimeOffset remoteLastModified, ConflictType conflictType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return new ConflictRecord
        {
            Id                 = Guid.NewGuid(),
            AccountId          = accountId,
            FilePath           = filePath,
            LocalLastModified  = localLastModified,
            RemoteLastModified = remoteLastModified,
            ConflictType       = conflictType,
            DetectedAt         = DateTimeOffset.UtcNow,
            IsResolved         = false
        };
    }
}
