using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AStar.Dev.Logging.Extensions;
using AStar.Dev.Utilities;
using Avalonia;
using Avalonia.Styling;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.Clock.Theming;

/// <summary>Applies theme visuals via Avalonia and persists the selected <see cref="ThemeMode" /> to <see cref="ApplicationDirectories.DataDirectory" />.</summary>
/// <param name="fileSystem">The file system abstraction used to persist the selected theme.</param>
/// <param name="logger">The logger used to report persistence failures.</param>
public sealed class ThemeService(IFileSystem fileSystem, ILogger<ThemeService> logger) : IThemeService
{
    private static readonly string settingsFilePath = ApplicationDirectories.DataDirectory.CombinePath("theme.json");
    private static readonly JsonSerializerOptions serializerOptions = new() { Converters = { new JsonStringEnumConverter() } };

    /// <inheritdoc />
    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.System;

    /// <inheritdoc />
    public void Initialize()
    {
        CurrentTheme = LoadPersistedTheme();
        ApplyVisuals(CurrentTheme);
    }

    /// <inheritdoc />
    public void ApplyTheme(ThemeMode themeMode)
    {
        CurrentTheme = themeMode;
        Persist(themeMode);
        ApplyVisuals(themeMode);
    }

    private ThemeMode LoadPersistedTheme()
    {
        if (!fileSystem.File.Exists(settingsFilePath))
        {
            return ThemeMode.System;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<ThemeSettings>(fileSystem.File.ReadAllText(settingsFilePath), serializerOptions);

            return settings?.Theme ?? ThemeMode.System;
        }
        catch (JsonException exception)
        {
            LogMessage.Warning(logger, nameof(ThemeService), $"Could not parse persisted theme settings: {exception.Message}");

            return ThemeMode.System;
        }
    }

    private void Persist(ThemeMode themeMode)
    {
        fileSystem.Directory.CreateDirectory(ApplicationDirectories.DataDirectory);
        fileSystem.File.WriteAllText(settingsFilePath, JsonSerializer.Serialize(new ThemeSettings(themeMode), serializerOptions));
    }

    private static void ApplyVisuals(ThemeMode themeMode)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = themeMode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private sealed record ThemeSettings(ThemeMode Theme);
}
