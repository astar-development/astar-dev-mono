using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Keeps the app-wide automatic sync schedule (#1403).</summary>
public interface ISyncScheduleSettings
{
    /// <summary>Reads the schedule.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The saved schedule, the default schedule when none was saved, or the error that stopped the read.</returns>
    Task<Result<SyncSchedule, ErrorResponse>> GetAsync(CancellationToken cancellationToken);

    /// <summary>Saves the schedule. The scheduler picks it up without a restart.</summary>
    /// <param name="schedule">The schedule to save.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The saved schedule, or the error that stopped the save.</returns>
    Task<Result<SyncSchedule, ErrorResponse>> SetAsync(SyncSchedule schedule, CancellationToken cancellationToken);
}
