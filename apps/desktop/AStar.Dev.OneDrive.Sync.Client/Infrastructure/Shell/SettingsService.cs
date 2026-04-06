using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Settings;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task SaveAsync();
    event EventHandler<AppSettings>? SettingsChanged;
}

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path = ApplicationMetadata.ApplicationName.ApplicationDirectory().CombinePath("settings.json");

    public AppSettings Current { get; private set; } = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public static async Task<SettingsService> LoadAsync()
    {
        var svc = new SettingsService();
        if (!File.Exists(svc._path)) return svc;

        try
        {
            await using var stream = File.OpenRead(svc._path);
            svc.Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, _jsonOpts) ?? new AppSettings();
        }
        catch
        {
            svc.Current = new AppSettings();
        }

        return svc;
    }

    public async Task SaveAsync()
    {
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, Current, _jsonOpts);
        SettingsChanged?.Invoke(this, Current);
    }
}
