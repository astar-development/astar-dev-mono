using Microsoft.Extensions.Logging;

namespace AStarDev.SingleInstance;

/// <summary>Claims the lock file and hands off to the running instance over a Unix domain socket.</summary>
/// <param name="logger">Reports hand-off results and lock failures, without paths.</param>
internal sealed class SingleInstanceGuard(ILogger<SingleInstanceGuard> logger) : ISingleInstanceGuard
{
    /// <inheritdoc />
    public Task<InstanceClaim> ClaimAsync(SingleInstanceOptions options, CancellationToken cancellationToken) => throw new NotImplementedException($"{logger}{options}{cancellationToken}");
}
