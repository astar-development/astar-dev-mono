using System.IO.Abstractions;
using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

/// <inheritdoc />
public sealed class SettingsService(IFileSystem fileSystem, ILogger<SettingsService> logger, string? settingsFilePath = null) : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string path = settingsFilePath ?? ApplicationMetadata.ApplicationNameHyphenated.ApplicationDirectory().CombinePath("settings.json");

    /// <inheritdoc />
    public AppSettings Current { get; private set; } = new();

    /// <inheritdoc />
    public event EventHandler<AppSettings>? SettingsChanged;

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        if (!fileSystem.File.Exists(path))
            return;

        try
        {
            await using var stream = fileSystem.File.OpenRead(path);
            Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOpts).ConfigureAwait(false) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            OneDriveSyncClientMessages.SettingsDeserializeFailed(logger, path, ex);
            Current = new AppSettings();
        }

        SettingsChanged?.Invoke(this, Current);
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await using var stream = fileSystem.File.Create(path);
        await JsonSerializer.SerializeAsync(stream, Current, JsonOpts).ConfigureAwait(false);
        SettingsChanged?.Invoke(this, Current);
    }
}
