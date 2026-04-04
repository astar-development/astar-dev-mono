using System;
using System.Diagnostics.CodeAnalysis;
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
internal sealed class CascadeService(IConflictStore store, ILogger<CascadeService> logger) : ICascadeService
{
    /// <inheritdoc />
    public async Task<Result<int, ConflictStoreError>> ApplyCascadeAsync(Guid resolvedConflictId, string filePath, ResolutionStrategy strategy, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var matchesResult = await store.GetByFilePathAsync(filePath, ct).ConfigureAwait(false);

        if (matchesResult is Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Error error)

            return new Result<int, ConflictStoreError>.Error(error.Reason);

        var matches = ((Result<System.Collections.Generic.IReadOnlyList<ConflictRecord>, ConflictStoreError>.Ok)matchesResult).Value;

        var cascadeCount = 0;

        foreach (var conflict in matches)
        {
            if (conflict.Id == resolvedConflictId)
                continue;

            var resolveResult = await store.ResolveAsync(conflict.Id, strategy, ct).ConfigureAwait(false);

            if (resolveResult is Result<ConflictRecord, ConflictStoreError>.Ok)
            {
                ConflictResolutionLogMessage.CascadeResolutionApplied(logger, conflict.Id, filePath, strategy);
                cascadeCount++;
            }
        }

        return new Result<int, ConflictStoreError>.Ok(cascadeCount);
    }
}
