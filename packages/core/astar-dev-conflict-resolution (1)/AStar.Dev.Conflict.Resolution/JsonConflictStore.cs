using System.Text.Json;

namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// JSON file-based implementation of <see cref="IConflictStore"/>.
/// Persists the conflict queue to a JSON file so that skipped
/// conflicts survive application restarts (CR-05).
/// </summary>
public sealed class JsonConflictStore : IConflictStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    /// <summary>
    /// Initialises a new instance of the <see cref="JsonConflictStore"/> class.
    /// </summary>
    /// <param name="filePath">Absolute path to the JSON file used for persistence.</param>
    public JsonConflictStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ConflictRecord>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var stream = File.OpenRead(_filePath);
        await using (stream.ConfigureAwait(false))
        {
            var records = await JsonSerializer.DeserializeAsync<List<ConflictRecord>>(stream, SerializerOptions, ct).ConfigureAwait(false);
            return records ?? [];
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(IReadOnlyList<ConflictRecord> records, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stream = File.Create(_filePath);
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, records, SerializerOptions, ct).ConfigureAwait(false);
        }
    }
}
