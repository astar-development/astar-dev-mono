namespace AStar.Dev.Sync.Engine.Features.Activity;

/// <summary>
///     Receives a notification after each file operation performed by the sync engine (S013).
///     The default registration is <see cref="NullActivityReporter"/>; the desktop app overrides
///     this with <c>ActivityFeedService</c> which surfaces events to the Activity view.
/// </summary>
public interface IActivityReporter
{
    /// <summary>Records a single file operation event.</summary>
    void Report(string accountId, ActivityActionType actionType, string filePath);
}
