using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Persistence;
using AStar.Dev.OneDrive.Sync.Client.Services.Settings;

namespace AStar.Dev.OneDrive.Sync.Client.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task SaveAsync();
    event EventHandler<AppSettings>? SettingsChanged;
}

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;

    public AppSettings Current { get; private set; } = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        string dir = new LocalApplicationPathsProvider().ApplicationDirectory;
        _path = Path.Combine(dir, "settings.json");
    }

    public SettingsService(IApplicationPathsProvider pathProvider)
        => _path = Path.Combine(pathProvider.ApplicationDirectory, "settings.json");

    public static async Task<SettingsService> LoadAsync()
    {
        var svc = new SettingsService();
        if (!File.Exists(svc._path)) return svc;

        try
        {
            await using var stream = File.OpenRead(svc._path);
            svc.Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOpts) ?? new AppSettings();
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
        await JsonSerializer.SerializeAsync(stream, Current, JsonOpts);
        SettingsChanged?.Invoke(this, Current);
    }
}
