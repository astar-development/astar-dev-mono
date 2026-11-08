using System.IO.Abstractions;
using System.Text.Json;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.DisplaySettings;

/// <summary>Keeps the <see cref="DisplayPreferences"/> in a small JSON file.</summary>
/// <param name="settingsFile">The JSON file holding the preferences.</param>
/// <param name="logger">Reports preferences that cannot be read or saved.</param>
internal sealed class JsonDisplayPreferencesStore(IFileInfo settingsFile, ILogger<JsonDisplayPreferencesStore> logger) : IDisplayPreferencesStore
{
    /// <inheritdoc />
    public DisplayPreferences Load()
    {
        settingsFile.Refresh();
        if (!settingsFile.Exists)
            return DisplayPreferencesFactory.CreateDefault();

        try
        {
            var stored = JsonSerializer.Deserialize<StoredPreferences>(settingsFile.FileSystem.File.ReadAllText(settingsFile.FullName));

            return DisplayPreferencesFactory.Create(MainLayoutFactory.FromKey(stored?.Layout), AppThemeFactory.FromKey(stored?.Theme));
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            DisplayPreferencesLog.PreferencesUnreadable(logger, exception.ErrorType());

            return DisplayPreferencesFactory.CreateDefault();
        }
    }

    /// <inheritdoc />
    public void Save(DisplayPreferences preferences)
    {
        try
        {
            settingsFile.Directory?.Create();
            settingsFile.FileSystem.File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new StoredPreferences(preferences.Layout.ToKey(), preferences.Theme.ToKey())));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DisplayPreferencesLog.PreferencesNotSaved(logger, exception.ErrorType());
        }
    }

    private sealed record StoredPreferences(string Layout, string Theme);
}
