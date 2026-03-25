using System.Text.Json;

namespace AStar.Dev.OneDrive.Client.Authentication;

/// <summary>
///     JSON file-backed implementation of <see cref="IConsentStore"/>.
///     Consent decisions are written to <c>consent.json</c> inside the
///     supplied directory and persist across application restarts (AU-03).
/// </summary>
public sealed class FileConsentStore : IConsentStore
{
    private const string ConsentFileName = "consent.json";

    private readonly Lock _lock = new();
    private readonly string _filePath;
    private Dictionary<string, bool> _consent;

    /// <summary>
    ///     Initialises a new instance of <see cref="FileConsentStore"/>,
    ///     loading any previously persisted consent decisions from
    ///     <paramref name="directory"/>.
    /// </summary>
    /// <param name="directory">Directory in which <c>consent.json</c> is stored.</param>
    public FileConsentStore(string directory)
    {
        _filePath = Path.Combine(directory, ConsentFileName);
        _consent  = Load();
    }

    /// <inheritdoc />
    public bool HasConsented(string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        lock(_lock)
        {
            return _consent.TryGetValue(accountId, out var consented) && consented;
        }
    }

    /// <inheritdoc />
    public void RecordConsent(string accountId, bool consented)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        lock(_lock)
        {
            _consent[accountId] = consented;
            Save();
        }
    }

    private Dictionary<string, bool> Load()
    {
        if(!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? [];
        }
        catch(JsonException)
        {
            return [];
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_consent));
    }
}
