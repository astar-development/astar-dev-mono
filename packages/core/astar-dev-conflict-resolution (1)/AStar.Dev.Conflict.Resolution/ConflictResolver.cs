using System.Globalization;

namespace AStar.Dev.Conflict.Resolution;

/// <summary>
/// Default implementation of <see cref="IConflictResolver"/>.
/// Persists the queue via <see cref="IConflictStore"/> and cascades
/// resolution decisions to matching file paths.
/// </summary>
public sealed class ConflictResolver : IConflictResolver
{
    private readonly IConflictStore _store;
    private List<ConflictRecord> _records = [];

    /// <summary>
    /// Initialises a new instance of the <see cref="ConflictResolver"/> class.
    /// </summary>
    /// <param name="store">The backing store for conflict persistence.</param>
    public ConflictResolver(IConflictStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>Loads the queue from the backing store. Call once at startup.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        var loaded = await _store.LoadAsync(ct).ConfigureAwait(false);
        _records = [.. loaded];
    }

    /// <inheritdoc/>
    public async Task AddConflictAsync(ConflictRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
        await _store.SaveAsync(_records, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConflictRecord>> GetPendingAsync(CancellationToken ct = default)
    {
        IReadOnlyList<ConflictRecord> pending = _records.Where(r => !r.IsResolved).ToList();
        return Task.FromResult(pending);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ConflictRecord>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<ConflictRecord> all = _records.ToList();
        return Task.FromResult(all);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Guid>> ResolveAsync(IReadOnlyList<Guid> conflictIds, ConflictPolicy policy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conflictIds);

        var now = DateTimeOffset.UtcNow;
        var resolvedIds = new List<Guid>();

        // Collect the file paths of the explicitly selected conflicts
        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in _records)
        {
            if (conflictIds.Contains(record.Id))
            {
                targetPaths.Add(record.FilePath);
            }
        }

        // Resolve the selected conflicts AND cascade to all pending
        // conflicts with the same file path (CR-08)
        foreach (var record in _records)
        {
            if (record.IsResolved)
            {
                continue;
            }

            var isDirectTarget = conflictIds.Contains(record.Id);
            var isCascadeTarget = targetPaths.Contains(record.FilePath);

            if (isDirectTarget || isCascadeTarget)
            {
                record.ResolvedPolicy = policy;
                record.ResolvedAtUtc = now;
                resolvedIds.Add(record.Id);
            }
        }

        await _store.SaveAsync(_records, ct).ConfigureAwait(false);
        return resolvedIds;
    }

    /// <inheritdoc/>
    public string GenerateKeepBothName(string originalFileName, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalFileName);

        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = utcNow.UtcDateTime.ToString("yyyy-MM-ddTHHmmssZ", CultureInfo.InvariantCulture);

        return $"{nameWithoutExtension}-({timestamp}){extension}";
    }
}
