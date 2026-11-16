using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Classification.TestsUnit.ClassificationTestData;

namespace AStarDev.CloudSync.Classification.TestsUnit;

internal sealed class CountingCategoryStore : ICategoryStore
{
    private int getAllCalls;

    public int GetAllCalls => Volatile.Read(ref getAllCalls);

    public Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure> Categories { get; set; } =
        new Ok<IReadOnlyList<ClassificationCategory>, ClassificationFailure>([ClassificationCategoryFactory.Create(Colour, "Colour")]);

    public Result<ClassificationCategory, ClassificationFailure> WriteResult { get; set; } = new Ok<ClassificationCategory, ClassificationFailure>(ClassificationCategoryFactory.Create(Place, "Place"));

    public Task<Result<IReadOnlyList<ClassificationCategory>, ClassificationFailure>> GetAllAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref getAllCalls);

        return Task.FromResult(Categories);
    }

    public Task<Result<ClassificationCategory, ClassificationFailure>> UpsertAsync(ClassificationCategory category, CancellationToken cancellationToken) => Task.FromResult(WriteResult);
}
