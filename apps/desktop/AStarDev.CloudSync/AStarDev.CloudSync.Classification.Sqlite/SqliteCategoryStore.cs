using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class SqliteCategoryStore : ICategoryStore
{
    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) => throw new NotImplementedException(nameof(GetAllAsync));

    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) => throw new NotImplementedException($"{nameof(UpsertAsync)} {category}");
}
