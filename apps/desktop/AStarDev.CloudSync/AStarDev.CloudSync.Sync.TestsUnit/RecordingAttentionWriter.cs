using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class RecordingAttentionWriter : ISyncAttentionWriter, ISyncAttentionReader, ISyncAttentionCloser
{
    private readonly List<SyncAttentionItem> recorded = [];
    private readonly List<SyncAttentionItem> seeded = [];
    private readonly List<HeldDeletionId> closed = [];
    private readonly List<(ConflictId Id, AttentionResolution Resolution)> closedConflicts = [];

    public bool Fails { get; set; }

    public bool ReadFails { get; set; }

    public bool CloseFails { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<SyncAttentionItem.HeldDeletions> Held => [.. recorded.OfType<SyncAttentionItem.HeldDeletions>()];

    public IReadOnlyList<SyncAttentionItem.Conflict> Conflicts => [.. recorded.OfType<SyncAttentionItem.Conflict>()];

    public IReadOnlyList<(ConflictId Id, AttentionResolution Resolution)> ClosedConflicts => closedConflicts;

    public IReadOnlyList<HeldDeletionId> Closed => closed;

    public void Seed(SyncAttentionItem item) => seeded.Add(item);

    public Task<Result<Unit, ErrorResponse>> RecordAsync(IReadOnlyList<SyncAttentionItem> items, CancellationToken cancellationToken)
    {
        Calls++;
        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        recorded.AddRange(items);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse>> GetOpenAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(ReadFails
            ? Result.Failure<IReadOnlyList<SyncAttentionItem>, ErrorResponse>(new ErrorResponse("locked"))
            : Result.Success<IReadOnlyList<SyncAttentionItem>, ErrorResponse>([.. seeded.Where(item => (item is not SyncAttentionItem.HeldDeletions held || !closed.Contains(held.Id)) && (item is not SyncAttentionItem.Conflict conflict || !closedConflicts.Any(done => done.Id == conflict.Id)))]));

    public Task<Result<IReadOnlyList<RelativeSyncPath>, ErrorResponse>> GetConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<AttentionCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ErrorResponse>> CloseAsync(ConflictId id, AttentionResolution resolution, CancellationToken cancellationToken)
    {
        if (CloseFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        closedConflicts.Add((id, resolution));

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<Unit, ErrorResponse>> CloseAsync(HeldDeletionId id, AttentionResolution resolution, CancellationToken cancellationToken)
    {
        if (CloseFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        closed.Add(id);

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }
}
