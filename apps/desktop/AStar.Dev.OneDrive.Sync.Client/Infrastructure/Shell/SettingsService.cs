using System.Text.Json;
using AStar.Dev.Utilities;
namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

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
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "[SettingsService] Failed to deserialize settings from {Path}; using defaults", svc._path);
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
