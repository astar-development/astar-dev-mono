using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Conflicts;

public sealed class FakeAttentionReader : ISyncAttentionReader
{
    private readonly Dictionary<AccountId, List<SyncAttentionItem>> items = [];

    public Func<AccountId, Exception?> Throws { get; set; } = _ => null;

    public Func<AccountId, ErrorResponse?> Fails { get; set; } = _ => null;

    public int Reads { get; private set; }

    public void Add(AccountId accountId, params SyncAttentionItem[] added)
    {
        if (!items.TryGetValue(accountId, out var list))
            items[accountId] = list = [];

        list.AddRange(added);
    }

    public void Remove(ConflictId id)
    {
        foreach (var list in items.Values)
            list.RemoveAll(item => item is SyncAttentionItem.Conflict conflict && conflict.Id == id);
    }

    public void RemoveHeld(HeldDeletionId id)
    {
        foreach (var list in items.Values)
            list.RemoveAll(item => item is SyncAttentionItem.HeldDeletions held && held.Id == id);
    }

    public Task<Result<IReadOnlyList<SyncAttentionItem>, ErrorResponse>> GetOpenAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        Reads++;
        var thrown = Throws(accountId);
        if (thrown is not null)
            throw thrown;

        var failure = Fails(accountId);
        if (failure is not null)
            return Task.FromResult(Result.Failure<IReadOnlyList<SyncAttentionItem>, ErrorResponse>(failure));

        IReadOnlyList<SyncAttentionItem> open = items.TryGetValue(accountId, out var list) ? [.. list] : [];

        return Task.FromResult(Result.Success<IReadOnlyList<SyncAttentionItem>, ErrorResponse>(open));
    }

    public Task<Result<IReadOnlyList<RelativeSyncPath>, ErrorResponse>> GetConflictedPathsAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<RelativeSyncPath>, ErrorResponse>([]));

    public Task<Result<AttentionCounts, ErrorResponse>> CountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<AttentionCounts, ErrorResponse>(new ErrorResponse("not used")));
}
