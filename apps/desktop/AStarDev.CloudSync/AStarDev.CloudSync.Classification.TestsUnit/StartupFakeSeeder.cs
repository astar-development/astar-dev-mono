namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class StartupFakeSeeder(List<string> calls) : IClassificationSeeder
{
    internal ClassificationSeedOutcome Outcome { get; init; } = ClassificationSeedOutcomeFactory.CreateApplied();

    internal bool Throws { get; init; }

    public Task<ClassificationSeedOutcome> SeedAsync(CancellationToken cancellationToken)
    {
        calls.Add("seed");

        return Throws ? throw new InvalidOperationException("seed exploded with C:\\secret\\path") : Task.FromResult(Outcome);
    }
}
