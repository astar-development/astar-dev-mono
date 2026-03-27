using System.Text.Json.Serialization;

namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// Represents a single file conflict detected during sync.
/// </summary>
public sealed class ConflictRecord
{
    /// <summary>Unique identifier for this conflict.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Relative path of the conflicting file (used for cascade matching).</summary>
    [JsonPropertyName("filePath")]
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Display-friendly file name.</summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = string.Empty;

    /// <summary>The type of conflict detected.</summary>
    [JsonPropertyName("conflictType")]
    public ConflictType ConflictType { get; init; }

    /// <summary>When the local file was last modified (null if deleted locally).</summary>
    [JsonPropertyName("localModifiedUtc")]
    public DateTimeOffset? LocalModifiedUtc { get; init; }

    /// <summary>When the remote file was last modified (null if deleted remotely).</summary>
    [JsonPropertyName("remoteModifiedUtc")]
    public DateTimeOffset? RemoteModifiedUtc { get; init; }

    /// <summary>Size of the local file in bytes (null if deleted locally).</summary>
    [JsonPropertyName("localSizeBytes")]
    public long? LocalSizeBytes { get; init; }

    /// <summary>Size of the remote file in bytes (null if deleted remotely).</summary>
    [JsonPropertyName("remoteSizeBytes")]
    public long? RemoteSizeBytes { get; init; }

    /// <summary>UTC timestamp when the conflict was first detected.</summary>
    [JsonPropertyName("detectedAtUtc")]
    public DateTimeOffset DetectedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The resolution policy chosen by the user. Null while unresolved.</summary>
    [JsonPropertyName("resolvedPolicy")]
    public ConflictPolicy? ResolvedPolicy { get; set; }

    /// <summary>UTC timestamp when the conflict was resolved. Null while unresolved.</summary>
    [JsonPropertyName("resolvedAtUtc")]
    public DateTimeOffset? ResolvedAtUtc { get; set; }

    /// <summary>Whether this conflict has been resolved.</summary>
    [JsonIgnore]
    public bool IsResolved => ResolvedPolicy is not null;
}
