using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ReclassifyFakeStore : IFileClassificationStore
{
    private readonly Dictionary<(AccountId, RelativeSyncPath), IReadOnlyList<TermId>> links = [];
    private readonly List<int> batchSizes = [];
    private readonly Lock gate = new();

    internal int FailOnBatch { get; init; }

    internal IReadOnlyList<int> BatchSizes => batchSizes;

    internal IReadOnlyDictionary<(AccountId, RelativeSyncPath), IReadOnlyList<TermId>> Links => links;

    internal TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task HoldWrite { get; init; } = Task.CompletedTask;

    public async Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        _ = WriteEntered.TrySetResult();
        await HoldWrite.WaitAsync(cancellationToken);
        return Replace(updates, cancellationToken);
    }

    private Result<Unit, ClassificationFailure> Replace(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (batchSizes.Count + 1 == FailOnBatch)
                return Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());

            batchSizes.Add(updates.Count);
            foreach (var update in updates)
                links[(update.AccountId, update.Path)] = update.TermIds;
        }

        return Result.Success<Unit, ClassificationFailure>(Unit.Instance);
    }

    internal void RemoveAccount(AccountId accountId)
    {
        lock (gate)
        {
            foreach (var key in links.Keys.Where(key => key.Item1 == accountId).ToList())
                _ = links.Remove(key);
        }
    }

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
