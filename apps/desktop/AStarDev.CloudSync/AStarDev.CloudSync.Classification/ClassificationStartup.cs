using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Migrates the database, which is safe to repeat, and then seeds the classification vocabulary. Nothing here is fatal: the application works without classification.</summary>
/// <param name="database">The database whose migration creates the classification tables.</param>
/// <param name="seeder">Writes the shipped seed.</param>
/// <param name="reclassifier">Reclassifies the baseline once a newly applied seed has changed the vocabulary.</param>
/// <param name="logger">Reports a migration or seed that failed, without paths or exception messages.</param>
public sealed class ClassificationStartup(ICloudSyncDatabase database, IClassificationSeeder seeder, IClassificationReclassifier reclassifier, ILogger<ClassificationStartup> logger) : IClassificationStartup
{
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
        _ = await seeder.SeedAsync(cancellationToken).ConfigureAwait(false);
        _ = reclassifier;

        return Unit.Instance;
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
