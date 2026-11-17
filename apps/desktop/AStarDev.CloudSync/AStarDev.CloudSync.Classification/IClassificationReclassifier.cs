namespace AStarDev.CloudSync.Classification;

/// <summary>Recomputes the automatic links of every file the baseline knows about. User links and ignored terms are untouched. A rerun is safe: it starts over and gives the same result.</summary>
public interface IClassificationReclassifier
{
    /// <summary>Runs off the calling thread. Never throws for a cancel or a failure; the outcome says what happened. A second call while a run is active returns AlreadyRunning.</summary>
    /// <param name="progress">Receives a report after each batch, including a batch whose files were all skipped, so the display never looks frozen.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    Task<ReclassifyOutcome> StartAsync(IProgress<ReclassifyProgress> progress, CancellationToken cancellationToken);
}
