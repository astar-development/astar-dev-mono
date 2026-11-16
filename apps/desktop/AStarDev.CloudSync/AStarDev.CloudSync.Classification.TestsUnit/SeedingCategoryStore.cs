using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.TestsUnit;

public sealed class SeedingCategoryStore : ICategoryStore
{
    private readonly List<ClassificationCategory> stored = [];

    public int UpsertCalls { get; private set; }

    public bool FailUpserts { get; init; }

    public bool ThrowOnUpsert { get; init; }

    public IReadOnlyList<ClassificationCategory> Stored => stored;

    public void Add(ClassificationCategory category) => stored.Add(category);

    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([.. stored]));

    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken)
    {
        UpsertCalls++;
        cancellationToken.ThrowIfCancellationRequested();
        if (ThrowOnUpsert)
            throw new InvalidOperationException("The store blew up.");

        if (FailUpserts)
            return Task.FromResult(Result.Failure<ClassificationCategory, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable()));

        var existing = stored.FirstOrDefault(item => string.Equals(item.Name, category.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            existing = category;
            stored.Add(category);
        }

        return Task.FromResult(Result.Success<ClassificationCategory, ClassificationFailure>(existing));
    }
}
