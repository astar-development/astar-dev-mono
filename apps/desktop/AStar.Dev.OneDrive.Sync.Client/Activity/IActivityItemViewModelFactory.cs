using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Activity;

/// <summary>Creates <see cref="ActivityItemViewModel"/> instances with their service dependencies resolved from the container.</summary>
public interface IActivityItemViewModelFactory
{
    /// <summary>Creates an activity item carrying only a file name, e.g. a sync-starting notice.</summary>
    ActivityItemViewModel Create(string fileName);

    /// <summary>Creates an informational activity item for the supplied account.</summary>
    ActivityItemViewModel CreateInfo(string accountId, string fileName);

    /// <summary>Creates an activity item describing a completed sync job.</summary>
    ActivityItemViewModel CreateFromJob(SyncJob job, string accountEmail);

    /// <summary>Creates an error activity item, e.g. when a manually triggered sync fails before it can start.</summary>
    ActivityItemViewModel CreateError(string accountId, string accountEmail, string message);
}
