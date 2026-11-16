using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FailingCategoryStore : ICategoryStore
{
    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<IReadOnlyList<ClassificationCategory>, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) => throw new NotSupportedException();
}
