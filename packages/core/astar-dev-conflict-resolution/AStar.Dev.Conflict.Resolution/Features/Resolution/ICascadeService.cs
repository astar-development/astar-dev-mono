using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Conflict.Resolution.Features.Resolution;

/// <summary>
///     When a conflict is resolved, applies the same resolution to all other pending conflicts
///     for the same file path across all accounts and sessions (CR-08).
/// </summary>
public interface ICascadeService
{
    /// <summary>
    ///     Finds all pending conflicts matching <paramref name="filePath"/> (excluding <paramref name="resolvedConflictId"/>)
    ///     and applies <paramref name="strategy"/> to each. Returns the number of cascaded resolutions.
    /// </summary>
    Task<Result<int, ConflictStoreError>> ApplyCascadeAsync(System.Guid resolvedConflictId, string filePath, ResolutionStrategy strategy, CancellationToken ct = default);
}
