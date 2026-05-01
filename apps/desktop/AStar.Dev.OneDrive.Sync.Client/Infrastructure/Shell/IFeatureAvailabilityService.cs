namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IFeatureAvailabilityService
{
    /// <summary>
    /// Determines whether the specified navigation section should be available based on current application state, configuration, and/or other factors.
     /// This allows for dynamic hiding/showing of certain UI sections without needing to hardcode visibility logic in the views themselves.
     /// For example, the "Logs" section may only be available if logging is enabled in the app settings, or the "Settings" section may be hidden for non-admin users.
    /// </summary>
    /// <param name="section">The navigation section to check for availability.</param>
    /// <returns><c>true</c> if the section is available; otherwise, <c>false</c>.</returns>
    bool IsAvailable(NavSection section);
}
