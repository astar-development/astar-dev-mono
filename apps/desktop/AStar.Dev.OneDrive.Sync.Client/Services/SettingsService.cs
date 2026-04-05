using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Data;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Settings;

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
        string dir = DbContextFactory.GetPlatformDataDirectory();
        _ = Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "settings.json");
    }

    public static async Task<SettingsService> LoadAsync()
    {
        var svc = new SettingsService();
        if(File.Exists(svc._path))
        {
            try
            {
                await using var stream = File.OpenRead(svc._path);
                svc.Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOpts) ?? new AppSettings();
            }
            catch
            {
                svc.Current = new AppSettings();
            }
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
