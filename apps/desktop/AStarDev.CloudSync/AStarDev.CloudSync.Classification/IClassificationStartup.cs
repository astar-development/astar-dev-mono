namespace AStarDev.CloudSync.Classification;

/// <summary>Prepares classification at application startup. Run it once, off the UI thread, before the first classification.</summary>
public interface IClassificationStartup
{
    /// <summary>Makes sure the database is migrated, then applies the shipped seed, and reclassifies the baseline once when the seed was newly applied. Never throws: a failure or a cancel is logged and the application carries on without a seeded vocabulary.</summary>
    /// <param name="cancellationToken">Cancels the run.</param>
    Task RunAsync(CancellationToken cancellationToken);
}
