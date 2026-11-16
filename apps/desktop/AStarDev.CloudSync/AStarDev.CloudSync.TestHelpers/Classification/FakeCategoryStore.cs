using AStarDev.CloudSync.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public sealed class FakeCategoryStore : ICategoryStore
{
    private readonly IReadOnlyList<ClassificationCategory> categories;

    public FakeCategoryStore(params ClassificationCategory[] categories) => this.categories = categories;

    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<ClassificationCategory>, ClassificationFailure>(categories));

    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) => throw new NotSupportedException();
}
