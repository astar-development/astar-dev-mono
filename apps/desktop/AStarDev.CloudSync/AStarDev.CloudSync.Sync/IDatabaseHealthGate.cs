namespace AStarDev.CloudSync.Sync;

/// <summary>Runs the <see cref="IDatabaseHealthCheck"/> once per app session and remembers the answer, so every sync run, the recovery and the overview agree on it (#1612). A failed check is kept until the app restarts.</summary>
public interface IDatabaseHealthGate
{
    /// <summary>Raised when the check finishes.</summary>
    event EventHandler? Changed;

    /// <summary>The answer so far; <see cref="DatabaseHealthStatus.NotChecked"/> until the first check finishes.</summary>
    DatabaseHealthStatus Status { get; }

    /// <summary>Runs the check if it has not finished yet, otherwise returns the remembered answer. Concurrent callers share one check. A cancelled check is not remembered and returns <see cref="DatabaseHealthStatus.NotChecked"/>.</summary>
    /// <param name="cancellationToken">Cancels the wait and the check.</param>
    Task<DatabaseHealthStatus> CheckAsync(CancellationToken cancellationToken);
}
