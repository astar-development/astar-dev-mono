using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="providers">Finds the provider capabilities.</param>
/// <param name="inputs">Reads the root, rules, baseline and remote index.</param>
/// <param name="planner">Plans the steps.</param>
/// <param name="executor">Runs the steps.</param>
/// <param name="logger">Reports the counts of a pass, with the hashed account id only.</param>
internal sealed class UploadPass(UploadPassProviders providers, UploadPassInputs inputs, IUploadPlanner planner, UploadItemExecutor executor, ILogger<UploadPass> logger) : IUploadPass
{
    /// <inheritdoc />
    public Task<Result<UploadPassSummary, UploadPassFailure>> RunAsync(AccountId accountId, LocalChangeSet changes, CancellationToken cancellationToken) =>
        throw new NotImplementedException($"{providers}{inputs}{planner}{executor}{logger}{accountId}{changes}{cancellationToken}");
}
