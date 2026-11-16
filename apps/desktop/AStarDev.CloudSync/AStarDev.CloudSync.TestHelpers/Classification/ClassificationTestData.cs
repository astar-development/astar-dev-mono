using AStarDev.CloudSync.Classification;

namespace AStarDev.CloudSync.TestHelpers.Classification;

public static class ClassificationTestData
{
    public static CategoryId NewCategoryId() => CategoryIdFactory.Create(Guid.NewGuid());

    public static ClassificationCategory Category(CategoryId id, string name) => ClassificationCategoryFactory.Create(id, name);

    public static ClassificationTerm Term(string text, CategoryId categoryId) => Term(text, categoryId, TermSourceFactory.CreateSeed(), TermStatusFactory.CreateActive());

    public static ClassificationTerm Term(string text, CategoryId categoryId, TermSource source, TermStatus status) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(Guid.NewGuid()), text, categoryId, source, status).Match(term => term, () => throw new InvalidOperationException());
}
