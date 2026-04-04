namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>Emits non-blocking toast notifications from the Dashboard feature (S012, SE-15).</summary>
public interface IToastService
{
    /// <summary>
    ///     Shows a non-blocking toast with the given <paramref name="message"/>.
    ///     <paramref name="accountId"/> is used to contextualise the notification (e.g. log viewer filter).
    /// </summary>
    void Show(string message, string accountId);
}
