using System.IO.Abstractions;
using System.Text.Json;
using AStarDev.FunctionalParadigm;
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

        return settingsFile.Exists ? Try.Run(Read).Match(preferences => preferences, Unreadable) : DisplayPreferencesFactory.CreateDefault();
    }

    /// <inheritdoc />
    public void Save(DisplayPreferences preferences) => _ = Try.Run(() => Write(preferences)).Match(written => written, NotSaved);

    private DisplayPreferences Read()
    {
        var stored = JsonSerializer.Deserialize(settingsFile.FileSystem.File.ReadAllText(settingsFile.FullName), DisplayPreferencesJsonContext.Default.StoredPreferences);

        return DisplayPreferencesFactory.Create(MainLayoutFactory.FromKey(stored?.Layout), AppThemeFactory.FromKey(stored?.Theme));
    }

    private Unit Write(DisplayPreferences preferences)
    {
        settingsFile.Directory?.Create();
        settingsFile.FileSystem.File.WriteAllText(settingsFile.FullName, JsonSerializer.Serialize(new StoredPreferences(preferences.Layout.ToKey(), preferences.Theme.ToKey()), DisplayPreferencesJsonContext.Default.StoredPreferences));

        return Unit.Instance;
    }

    private DisplayPreferences Unreadable(Exception exception)
    {
        DisplayPreferencesLog.PreferencesUnreadable(logger, exception.ErrorType());

        return DisplayPreferencesFactory.CreateDefault();
    }

    private Unit NotSaved(Exception exception)
    {
        DisplayPreferencesLog.PreferencesNotSaved(logger, exception.ErrorType());

        return Unit.Instance;
    }

    internal sealed record StoredPreferences(string Layout, string Theme);
}
