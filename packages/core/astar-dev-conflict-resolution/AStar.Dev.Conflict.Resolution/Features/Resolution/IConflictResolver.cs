using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.Conflict.Resolution.Domain;
using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.Conflict.Resolution.Features.Resolution;

/// <summary>
///     Executes a chosen resolution strategy against a conflict record (CR-03, CR-04).
///     Never throws — all failures are returned as <see cref="ConflictResolverError"/>.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    ///     Executes <paramref name="strategy"/> for <paramref name="conflict"/>.
    ///     LocalWins / RemoteWins deletions are logged at Warning before execution (NF-04).
    /// </summary>
    Task<Result<ConflictRecord, ConflictResolverError>> ResolveAsync(ConflictRecord conflict, ResolutionStrategy strategy, CancellationToken ct = default);
}
