using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Infrastructure;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Conflict.Resolution.Features.Detection;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI.")]
internal sealed class ConflictDetector(IFileSystem fileSystem, ILogger<ConflictDetector> logger) : IConflictDetector
{
    /// <inheritdoc />
    public Task<Result<ConflictRecord?, ConflictDetectionError>> DetectAsync(Guid accountId, string filePath, DateTimeOffset? remoteLastModified, long? remoteSize, bool isRemoteDeleted, DateTimeOffset? lastSyncCompletedAt, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        try
        {
            var result = DetectConflict(accountId, filePath, remoteLastModified, remoteSize, isRemoteDeleted, lastSyncCompletedAt);

            if (result is not null)
                ConflictResolutionLogMessage.ConflictDetected(logger, filePath, accountId, result.ConflictType, result.Id);

            return Task.FromResult<Result<ConflictRecord?, ConflictDetectionError>>(new Result<ConflictRecord?, ConflictDetectionError>.Ok(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<Result<ConflictRecord?, ConflictDetectionError>>(
                new Result<ConflictRecord?, ConflictDetectionError>.Error(ConflictDetectionErrorFactory.AccessDenied(filePath)));
        }
    }

    private ConflictRecord? DetectConflict(Guid accountId, string filePath, DateTimeOffset? remoteLastModified, long? remoteSize, bool isRemoteDeleted, DateTimeOffset? lastSyncCompletedAt)
    {
        var localExists = fileSystem.File.Exists(filePath);

        if (isRemoteDeleted && localExists)

            return BuildConflict(accountId, filePath, fileSystem.FileInfo.New(filePath).LastWriteTimeUtc, DateTimeOffset.MinValue, ConflictType.DeletedOnOneSide);

        if (!localExists || remoteLastModified is null)

            return null;

        var localInfo        = fileSystem.FileInfo.New(filePath);
        var localLastModified = new DateTimeOffset(localInfo.LastWriteTimeUtc, TimeSpan.Zero);

        var localWasModifiedSinceLastSync = lastSyncCompletedAt is null || localLastModified > lastSyncCompletedAt.Value;
        var remoteIsNewer                 = remoteLastModified.Value > localLastModified;
        var sizeMismatch                  = remoteSize is not null && localInfo.Length != remoteSize.Value;
        var remoteHasChanges              = remoteIsNewer || sizeMismatch;

        if (remoteHasChanges && localWasModifiedSinceLastSync)

            return BuildConflict(accountId, filePath, localLastModified, remoteLastModified.Value, ConflictType.BothModified);

        return null;
    }

    private static ConflictRecord BuildConflict(Guid accountId, string filePath, DateTimeOffset localLastModified, DateTimeOffset remoteLastModified, ConflictType conflictType)

        => ConflictRecordFactory.Create(accountId, filePath, localLastModified, remoteLastModified, conflictType);
}
