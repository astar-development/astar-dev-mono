using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Classification.Sqlite;

internal static class TermMapping
{
    public static TermEntity ToEntity(ClassificationTerm term) => new()
    {
        Id = term.Id.Value,
        Text = term.Text,
        CategoryId = term.CategoryId.Value,
        Source = SourceName(term.Source),
        IsIgnored = term.Status is TermStatus.Ignored
    };

    public static ClassificationTerm ToTerm(TermEntity entity) =>
        ClassificationTermFactory.Create(TermIdFactory.Create(entity.Id), entity.Text, CategoryIdFactory.Create(entity.CategoryId), SourceOf(entity.Source), entity.IsIgnored ? TermStatusFactory.CreateIgnored() : TermStatusFactory.CreateActive())
            .Match(term => term, () => throw Corrupt());

    public static ClassificationCategory ToCategory(CategoryEntity entity) => ClassificationCategoryFactory.Create(CategoryIdFactory.Create(entity.Id), entity.Name);

    public static InvalidDataException Corrupt() => new("A stored classification row is invalid.");

    private static string SourceName(TermSource source) => source switch
    {
        TermSource.Seed => nameof(TermSource.Seed),
        TermSource.User => nameof(TermSource.User),
        TermSource.Auto => nameof(TermSource.Auto),
        TermSource.Suggested => nameof(TermSource.Suggested),
        _ => throw Corrupt()
    };

    private static TermSource SourceOf(string name) => name switch
    {
        nameof(TermSource.Seed) => TermSourceFactory.CreateSeed(),
        nameof(TermSource.User) => TermSourceFactory.CreateUser(),
        nameof(TermSource.Auto) => TermSourceFactory.CreateAuto(),
        nameof(TermSource.Suggested) => TermSourceFactory.CreateSuggested(),
        _ => throw Corrupt()
    };
}
