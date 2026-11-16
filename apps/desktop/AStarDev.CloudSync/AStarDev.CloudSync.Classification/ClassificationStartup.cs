using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Classification;

/// <summary>Migrates the database, which is safe to repeat, and then seeds the classification vocabulary. Nothing here is fatal: the application works without classification.</summary>
/// <param name="database">The database whose migration creates the classification tables.</param>
/// <param name="seeder">Writes the shipped seed.</param>
/// <param name="logger">Reports a migration or seed that failed, without paths or exception messages.</param>
public sealed class ClassificationStartup(ICloudSyncDatabase database, IClassificationSeeder seeder, ILogger<ClassificationStartup> logger) : IClassificationStartup
{
    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(cancellationToken)} {database} {seeder} {logger}");
}
