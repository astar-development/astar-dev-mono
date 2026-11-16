using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class ClassifyHookStore : IFileClassificationStore
{
    private readonly List<FileTermLinks> replaced = [];
    private readonly List<string> removed = [];

    internal Func<Exception?> Throws { get; init; } = () => null;

    internal bool Fails { get; init; }

    internal IReadOnlyList<FileTermLinks> Replaced
    {
        get
        {
            lock (replaced)
                return [.. replaced];
        }
    }

    internal IReadOnlyList<string> Removed
    {
        get
        {
            lock (removed)
                return [.. removed];
        }
    }

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(IReadOnlyList<FileTermLinks> updates, CancellationToken cancellationToken)
    {
        var exception = Throws();
        if (exception is not null)
            throw exception;

        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        lock (replaced)
            replaced.AddRange(updates);

        return Task.FromResult(Result.Success<Unit, ClassificationFailure>(Unit.Instance));
    }

    public Task<Result<Unit, ClassificationFailure>> RemoveAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken)
    {
        var exception = Throws();
        if (exception is not null)
            throw exception;

        if (Fails)
            return Task.FromResult(Result.Failure<Unit, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        lock (removed)
            removed.Add(path.Value ?? string.Empty);

        return Task.FromResult(Result.Success<Unit, ClassificationFailure>(Unit.Instance));
    }

    public Task<Result<Unit, ClassificationFailure>> ReplaceAutoLinksAsync(AccountId accountId, RelativeSyncPath path, IReadOnlyList<TermId> termIds, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<Unit, ClassificationFailure>> AssignUserLinkAsync(AccountId accountId, RelativeSyncPath path, TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<TermId>, ClassificationFailure>> GetTermsForFileAsync(AccountId accountId, RelativeSyncPath path, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyList<ClassifiableFile>, ClassificationFailure>> GetFilesForTermAsync(TermId termId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Result<IReadOnlyDictionary<TermId, int>, ClassificationFailure>> GetFileCountsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}
