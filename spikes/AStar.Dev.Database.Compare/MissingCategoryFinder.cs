namespace AStar.Dev.Database.Compare;

public static class MissingCategoryFinder
{
    public static IReadOnlyList<(string, bool)> FindMissing(IReadOnlyList<(string, bool)> namesToCheck, IReadOnlyList<(string, bool)> referenceNames)
    {
        var referenceSet = new HashSet<string>(referenceNames.Select(x => x.Item1), StringComparer.OrdinalIgnoreCase);

        return namesToCheck.Where(name => !referenceSet.Contains(name.Item1)).Where(n=>n.Item2).ToList();
    }
}
