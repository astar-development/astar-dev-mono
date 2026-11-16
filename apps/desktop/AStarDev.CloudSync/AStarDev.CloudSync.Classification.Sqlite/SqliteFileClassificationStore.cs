using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class SqliteFileClassificationStore : IFileClassificationStore
{
    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(ReplaceAutoLinksAsync)} {accountId} {path} {termIds.Count}");

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(ReplaceAutoLinksAsync)} {updates.Count}");

    public Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(AssignUserLinkAsync)} {accountId} {path} {termId}");

    public Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(GetTermsForFileAsync)} {accountId} {path}");

    public Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(GetFilesForTermAsync)} {termId}");

    public Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(RemoveAsync)} {accountId} {path}");

    public Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken) => throw new NotImplementedException(nameof(GetFileCountsAsync));
}
