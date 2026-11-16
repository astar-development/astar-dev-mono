using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeFileClassificationStore : IFileClassificationStore
{
    public Dictionary<TermId, int> Counts { get; } = [];

    public bool FailGetFileCounts { get; set; }

    public Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(FailGetFileCounts
            ? Result.Failure<IReadOnlyDictionary<TermId, int>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable())
            : Result.Success<IReadOnlyDictionary<TermId, int>, ClassificationFailure>(new Dictionary<TermId, int>(Counts)));

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();
}
