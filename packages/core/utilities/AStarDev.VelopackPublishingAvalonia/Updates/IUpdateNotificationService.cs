using AStar.Dev.Velopack.Publishing;

namespace AStar.Dev.Velopack.Publishing.Avalonia.Updates;

/// <summary>Checks for an application update and, if one is found, presents the user with the update-available dialog.</summary>
public interface IUpdateNotificationService
{
    /// <summary>Checks for updates and shows the dialog if one is available. No-op if none is found or the check fails.</summary>
    Task CheckAndNotifyAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks for updates, shows the dialog if one is available, and returns the outcome so the caller can report it.</summary>
    /// <param name="cancellationToken">A token to cancel the check.</param>
    Task<UpdateCheckResult> CheckNowAsync(CancellationToken cancellationToken = default);
}
