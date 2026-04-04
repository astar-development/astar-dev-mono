using System;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Conflict.Resolution.Features.Detection;

/// <summary>
///     Detects sync conflicts between a remote delta item and the local file system.
///     Integrated into the sync engine during delta processing (CR-01).
/// </summary>
public interface IConflictDetector
{
    /// <summary>
    ///     Inspects the remote item and the local file state to determine whether a conflict exists.
    ///     Returns <see langword="null"/> in the success value when no conflict is detected.
    /// </summary>
    /// <param name="accountId">Synthetic account Guid.</param>
    /// <param name="filePath">Local file path of the item being processed.</param>
    /// <param name="remoteLastModified">When the remote copy was last modified; null for deleted items.</param>
    /// <param name="remoteSize">Size of the remote file in bytes; null for deleted items.</param>
    /// <param name="isRemoteDeleted">True when the remote delta indicates the item was deleted.</param>
    /// <param name="lastSyncCompletedAt">UTC time of the last completed sync; used to detect local modifications since last sync.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<ConflictRecord?, ConflictDetectionError>> DetectAsync(Guid accountId, string filePath, DateTimeOffset? remoteLastModified, long? remoteSize, bool isRemoteDeleted, DateTimeOffset? lastSyncCompletedAt, CancellationToken ct = default);
}
