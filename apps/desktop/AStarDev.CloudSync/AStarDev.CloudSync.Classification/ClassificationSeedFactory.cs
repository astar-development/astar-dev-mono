namespace AStarDev.CloudSync.Classification;

/// <summary>Creates <see cref="ClassificationSeed"/> values.</summary>
public static class ClassificationSeedFactory
{
    /// <summary>Creates a seed. Terms naming an unknown category are dropped, repeated terms of the same kind are removed ignoring case, and a blank version becomes 0.</summary>
    /// <param name="version">The version.</param>
    /// <param name="categories">The categories.</param>
    /// <param name="terms">The terms.</param>
    public static ClassificationSeed Create(string version, IReadOnlyList<SeedCategory> categories, IReadOnlyList<SeedTerm> terms) => Build(version, categories, terms);
    private static ClassificationSeed Build(string version, IReadOnlyList<SeedCategory> categories, IReadOnlyList<SeedTerm> terms)
    {
        var known = categories.Select(category => category.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var kept = terms
            .Where(term => known.Contains(term.CategoryName))
            .DistinctBy(term => (term.Text, term.CategoryName.ToUpperInvariant(), term.Kind));

        return new(string.IsNullOrWhiteSpace(version) ? "0" : version.Trim(), [.. categories.DistinctBy(category => category.Name.ToUpperInvariant())], [.. kept]);
    }
}
