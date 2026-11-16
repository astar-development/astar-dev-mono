using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ReclassifyFakeStore : IFileClassificationStore
{
    private readonly Dictionary<(AccountId, RelativeSyncPath), IReadOnlyList<TermId>> links = [];
    private readonly List<int> batchSizes = [];

    internal int FailOnBatch { get; init; }

    internal IReadOnlyList<int> BatchSizes => batchSizes;

    internal IReadOnlyDictionary<(AccountId, RelativeSyncPath), IReadOnlyList<TermId>> Links => links;

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batchSizes.Count + 1 == FailOnBatch)
            return Task.FromResult(Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        batchSizes.Add(updates.Count);
        foreach (var update in updates)
            links[(update.AccountId, update.Path)] = update.TermIds;

        return Task.FromResult(Result.Success<Unit, ClassificationFailure>(Unit.Instance));
    }

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
