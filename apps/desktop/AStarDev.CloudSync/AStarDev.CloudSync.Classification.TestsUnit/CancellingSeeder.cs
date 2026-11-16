namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class CancellingSeeder(CancellationTokenSource source) : IClassificationSeeder
{
    public async Task<ClassificationSeedOutcome> SeedAsync(CancellationToken cancellationToken)
    {
        await source.CancelAsync();

        throw new InvalidOperationException("seed stopped while the app was closing");
    }
}
