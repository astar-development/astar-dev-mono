using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification;

/// <summary>Stale when no run has completed yet, or when the last completed run used another engine version or another seed version than the one applied now. Versions compare ordinally, as the seeder compares them.</summary>
/// <param name="runs">Reads the last completed run.</param>
/// <param name="seedState">Reads the applied seed version.</param>
public sealed class ReclassifyFreshnessCheck(IReclassifyRunStore runs, ISeedStateStore seedState) : IReclassifyFreshnessCheck
{
    /// <inheritdoc />
    public Task<Result<ReclassifyFreshness, ClassificationFailure>> CheckAsync(CancellationToken cancellationToken) =>
        seedState.GetAppliedVersionAsync(cancellationToken).BindAsync(applied => runs.GetLastCompletedAsync(cancellationToken).BindAsync(last => Task.FromResult(Result.Success<ReclassifyFreshness, ClassificationFailure>(FreshnessOf(last, applied)))));

    private static ReclassifyFreshness FreshnessOf(Option<ReclassifyRunRecord> last, Option<string> appliedSeed) =>
        last.Match(run => run.EngineVersion == ClassificationEngineVersion.Current && run.SeedVersion == appliedSeed, () => false)
            ? ReclassifyFreshnessFactory.CreateUpToDate()
            : ReclassifyFreshnessFactory.CreateStale();
}
