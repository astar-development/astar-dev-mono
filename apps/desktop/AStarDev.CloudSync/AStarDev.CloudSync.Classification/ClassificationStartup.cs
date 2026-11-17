using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Migrates the database, which is safe to repeat, and then seeds the classification vocabulary. When the seed is newly applied, or no reclassify run has ever been recorded (an install from before runs were recorded), the baseline is reclassified once, so files recorded before the seed finished are classified against the full vocabulary (#1870). When a manual run is already active, the startup reclassify waits for it to end and then runs again, so a run that started against a partial vocabulary is always followed by a full one (#1876). Nothing here is fatal: the application works without classification.</summary>
/// <param name="database">The database whose migration creates the classification tables.</param>
/// <param name="seeder">Writes the shipped seed.</param>
/// <param name="reclassifier">Reclassifies the baseline once a newly applied seed has changed the vocabulary.</param>
/// <param name="runs">Tells whether any reclassify run has ever completed.</param>
/// <param name="logger">Reports a migration or seed that failed, without paths or exception messages.</param>
public sealed class ClassificationStartup(ICloudSyncDatabase database, IClassificationSeeder seeder, IClassificationReclassifier reclassifier, IReclassifyRunStore runs, ILogger<ClassificationStartup> logger) : IClassificationStartup
{
    private readonly IReclassifyRunStore runStore = runs;

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var run = Try.RunAsync(() => MigrateAndSeedAsync(cancellationToken), cancellationToken).MatchAsync(_ => Unit.Instance, exception => Failed(exception, cancellationToken));
        await ((Task)run).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private Task<Unit> MigrateAndSeedAsync(CancellationToken cancellationToken) =>
        database.MigrateAsync(cancellationToken).MatchAsync(_ => SeedAsync(cancellationToken), _ => Task.FromResult(MigrationFailed(cancellationToken)));

    private async Task<Unit> SeedAsync(CancellationToken cancellationToken)
    {
        var outcome = await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        if (await ShouldReclassifyAsync(outcome, cancellationToken).ConfigureAwait(false))
            await ReclassifyAfterActiveRunAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Instance;
    }

    private async Task<bool> ShouldReclassifyAsync(ClassificationSeedOutcome outcome, CancellationToken cancellationToken)
    {
        if (outcome is ClassificationSeedOutcome.Applied)
            return true;

        if (outcome is not ClassificationSeedOutcome.AlreadyCurrent)
            return false;

        return await runStore.GetLastCompletedAsync(cancellationToken).MatchAsync(last => !last.Match(_ => true, () => false), _ => false).ConfigureAwait(false);
    }

    private async Task ReclassifyAfterActiveRunAsync(CancellationToken cancellationToken)
    {
        var outcome = await reclassifier.StartAsync(new Progress<ReclassifyProgress>(), cancellationToken).ConfigureAwait(false);
        while (outcome is ReclassifyOutcome.AlreadyRunning && !cancellationToken.IsCancellationRequested)
        {
            await reclassifier.WaitUntilIdleAsync(cancellationToken).ConfigureAwait(false);
            outcome = await reclassifier.StartAsync(new Progress<ReclassifyProgress>(), cancellationToken).ConfigureAwait(false);
        }
    }

    private Unit MigrationFailed(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Unit.Instance;

        LogMessage.Warning(logger, nameof(ClassificationStartup), "The classification seed was skipped because the database could not be migrated");

        return Unit.Instance;
    }

    private Unit Failed(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Unit.Instance;

        LogMessage.Warning(logger, nameof(ClassificationStartup), "Classification startup did not finish", exception.ErrorType());

        return Unit.Instance;
    }
}
