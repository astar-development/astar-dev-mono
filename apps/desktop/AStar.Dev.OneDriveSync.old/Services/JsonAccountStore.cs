using System.Text.Json;
using AStar.Dev.OneDriveSync.old.Models;

namespace AStar.Dev.OneDriveSync.old.Services;

/// <summary>AM-01 → AM-08: JSON-file-backed account store in AppData.</summary>
public sealed class JsonAccountStore : IAccountStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly string _filePath;

    public JsonAccountStore()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AStar.Dev.OneDriveSync.old");
        _ = Directory.CreateDirectory(appData);
        _filePath = Path.Combine(appData, "accounts.json");
    }

    public async Task<IReadOnlyList<AccountRecord>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        await using FileStream stream = File.OpenRead(_filePath);
        List<AccountRecord>? accounts = await JsonSerializer.DeserializeAsync<List<AccountRecord>>(stream, JsonOptions, ct);
        return accounts ?? [];
    }

    public async Task SaveAsync(IReadOnlyList<AccountRecord> accounts, CancellationToken ct = default)
    {
        await using FileStream stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, accounts, JsonOptions, ct);
    }
}
