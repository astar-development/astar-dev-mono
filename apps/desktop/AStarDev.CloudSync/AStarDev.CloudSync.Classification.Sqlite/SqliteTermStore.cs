using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class SqliteTermStore : ITermStore
{
    public Task<Result<IReadOnlyList<ClassificationTerm>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException(nameof(GetAllAsync));

    public Task<Result<ClassificationTerm, ClassificationFailure>> UpsertAsync(ClassificationTerm term, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(UpsertAsync)} {term}");

    public Task<Result<Option<ClassificationTerm>, ClassificationFailure>> GetByIdAsync(TermId termId, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(GetByIdAsync)} {termId}");

    public Task<Result<ClassificationTerm, ClassificationFailure>> SetStatusAsync(TermId termId, TermStatus status, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(SetStatusAsync)} {termId} {status}");
}
