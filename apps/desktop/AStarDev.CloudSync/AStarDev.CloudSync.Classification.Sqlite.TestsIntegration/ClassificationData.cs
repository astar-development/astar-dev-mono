using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public static class ClassificationData
{
    public static ClassificationCategory Category(string name) => ClassificationCategoryFactory.Create(CategoryIdFactory.Create(Guid.NewGuid()), name);

    public static ClassificationTerm Term(string text, CategoryId categoryId, TermStatus? status = null) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), text, categoryId, TermSourceFactory.CreateSeed(), status ?? TermStatusFactory.CreateActive()).Match(term => term, () => throw new InvalidOperationException("The test term must be valid."));

    public static RelativeSyncPath Path(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    public static AccountId NewAccount() => AccountIdFactory.Create(Guid.NewGuid());

    public static T Ok<T>(Result<T, ClassificationFailure> result) => result.Match(value => value, failure => throw new InvalidOperationException($"The call failed with {failure.GetType().Name}."));

    public static ClassificationFailure Failed<T>(Result<T, ClassificationFailure> result) => result.Match(_ => throw new InvalidOperationException("The call must fail."), failure => failure);
}
