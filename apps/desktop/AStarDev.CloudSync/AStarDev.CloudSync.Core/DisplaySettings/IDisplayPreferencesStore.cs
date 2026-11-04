namespace AStarDev.CloudSync.Core.DisplaySettings;

/// <summary>Loads and saves the user's <see cref="DisplayPreferences"/>.</summary>
public interface IDisplayPreferencesStore
{
    /// <summary>Loads the saved preferences, or the defaults when none are saved or they cannot be read.</summary>
    /// <returns>The preferences to apply at start-up.</returns>
    DisplayPreferences Load();

    /// <summary>Saves the preferences for the next start.</summary>
    /// <param name="preferences">The preferences to save.</param>
    void Save(DisplayPreferences preferences);
}
