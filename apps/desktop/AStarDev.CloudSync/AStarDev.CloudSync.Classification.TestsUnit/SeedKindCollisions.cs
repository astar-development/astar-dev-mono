namespace AStarDev.CloudSync.Classification.TestsUnit;

public static class SeedKindCollisions
{
    public static IReadOnlyList<string> Find(IEnumerable<SeedTerm> terms) => [.. terms
        .GroupBy(term => (term.Text, Category: term.CategoryName.ToLowerInvariant()))
        .Where(group => group.Select(term => term.Kind.GetType()).Distinct().Count() > 1)
        .Select(group => $"{group.Key.Text}|{group.First().CategoryName}")
        .Order(StringComparer.Ordinal)];
}
