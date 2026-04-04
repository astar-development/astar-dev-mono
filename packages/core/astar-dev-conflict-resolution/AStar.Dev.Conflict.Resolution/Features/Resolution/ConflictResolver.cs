using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Conflict.Resolution.Features.Persistence;
using AStar.Dev.Conflict.Resolution.Infrastructure;
using AStar.Dev.Functional.Extensions;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Conflict.Resolution.Features.Resolution;

/// <inheritdoc />
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Registered via DI.")]
internal sealed class ConflictResolver(IConflictStore store, IFileSystem fileSystem, ILogger<ConflictResolver> logger) : IConflictResolver
{
    /// <inheritdoc />
    public async Task<Result<ConflictRecord, ConflictResolverError>> ResolveAsync(ConflictRecord conflict, ResolutionStrategy strategy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        try
        {
            await ExecuteStrategyAsync(conflict, strategy, ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            return new Result<ConflictRecord, ConflictResolverError>.Error(
                ConflictResolverErrorFactory.FileOperationFailed(conflict.FilePath, ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return new Result<ConflictRecord, ConflictResolverError>.Error(
                ConflictResolverErrorFactory.FileOperationFailed(conflict.FilePath, ex.Message));
        }

        if (strategy != ResolutionStrategy.Skip)
            await store.ResolveAsync(conflict.Id, strategy, ct).ConfigureAwait(false);

        ConflictResolutionLogMessage.ConflictResolved(logger, conflict.Id, conflict.FilePath, strategy);

        conflict.IsResolved      = strategy != ResolutionStrategy.Skip;
        conflict.AppliedStrategy = strategy;

        return new Result<ConflictRecord, ConflictResolverError>.Ok(conflict);
    }

    private async Task ExecuteStrategyAsync(ConflictRecord conflict, ResolutionStrategy strategy, CancellationToken ct)
    {
        switch (strategy)
        {
            case ResolutionStrategy.LocalWins:
                ConflictResolutionLogMessage.DestructiveResolutionPending(logger, "discard-remote", conflict.FilePath, conflict.Id);
                break;

            case ResolutionStrategy.RemoteWins:
                ConflictResolutionLogMessage.DestructiveResolutionPending(logger, "overwrite-local", conflict.FilePath, conflict.Id);
                break;

            case ResolutionStrategy.KeepBoth:
                RenameConflictingCopy(conflict);
                break;

            case ResolutionStrategy.Skip:
                break;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void RenameConflictingCopy(ConflictRecord conflict)
    {
        if (!fileSystem.File.Exists(conflict.FilePath))

            return;

        var renamedPath = BuildKeepBothPath(conflict.FilePath, conflict.DetectedAt);
        fileSystem.File.Move(conflict.FilePath, renamedPath);
        ConflictResolutionLogMessage.ConflictCopyRenamed(logger, renamedPath, conflict.Id);
    }

    internal static string BuildKeepBothPath(string filePath, DateTimeOffset detectedAt)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var timestamp = detectedAt.UtcDateTime.ToString("yyyy-MM-ddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture);

        return Path.Combine(directory, $"{fileNameWithoutExt}-({timestamp}){extension}");
    }
}
