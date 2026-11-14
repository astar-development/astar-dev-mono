using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class InMemorySyncBaseline : ISyncBaselineReader, ISyncBaselineWriter
{
    private readonly Lock gate = new();
    private readonly SortedDictionary<string, SyncBaselineEntry> stored = new(StringComparer.OrdinalIgnoreCase);

    public int Pages { get; private set; }

    public int LargestPageReturned { get; private set; }

    public int Upserts { get; private set; }

    public bool UpsertFails { get; set; }

    public bool RemoveFails { get; set; }

    public IReadOnlyList<SyncBaselineEntry> All
    {
        get
        {
            lock (gate)
                return [.. stored.Values];
        }
    }

    public bool Has(string path)
    {
        lock (gate)
            return stored.ContainsKey(path);
    }

    public SyncBaselineEntry Find(string path)
    {
        lock (gate)
            return stored[path];
    }

    public void Add(SyncBaselineEntry entry)
    {
        lock (gate)
            stored[entry.Path.Value ?? string.Empty] = entry;
    }

    public Task<Result<Option<SyncBaselineEntry>, ErrorResponse>> GetAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<Option<SyncBaselineEntry>, ErrorResponse>(Has(path.Value ?? string.Empty) ? Option.Some(Find(path.Value ?? string.Empty)) : Option.None<SyncBaselineEntry>()));

    public Task<Result<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>> GetUnderAsync(AccountId accountId, RelativeSyncPath under, Option<RelativeSyncPath> after, int limit, CancellationToken cancellationToken)
    {
        Pages++;
        var prefix = under.Value is { Length: > 0 } folder ? folder + "/" : string.Empty;
        var cursor = after.Match(path => (string?)path.Value, () => null);
        IReadOnlyList<SyncBaselineEntry> page;
        lock (gate)
            page = [.. stored.Values.Where(entry => prefix.Length == 0 || entry.Path.Value!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || string.Equals(entry.Path.Value, under.Value, StringComparison.OrdinalIgnoreCase)).Where(entry => cursor is null || StringComparer.OrdinalIgnoreCase.Compare(entry.Path.Value, cursor) > 0).Take(limit)];
        LargestPageReturned = Math.Max(LargestPageReturned, page.Count);

        return Task.FromResult(Result.Success<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>(page));
    }

    public Task<Result<Unit, ErrorResponse>> UpsertAsync(AccountId accountId, IReadOnlyList<SyncBaselineEntry> entries, CancellationToken cancellationToken)
    {
        Upserts++;
        if (UpsertFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        foreach (var entry in entries)
            Add(entry);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<Unit, ErrorResponse>> RemoveAsync(AccountId accountId, IReadOnlyList<RelativeSyncPath> paths, CancellationToken cancellationToken)
    {
        if (RemoveFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        lock (gate)
            foreach (var path in paths)
                _ = stored.Remove(path.Value ?? string.Empty);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
