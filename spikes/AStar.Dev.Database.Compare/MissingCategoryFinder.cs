namespace AStar.Dev.Database.Compare;

public static class MissingCategoryFinder
{
    public static IReadOnlyList<string> FindMissing(IReadOnlyList<string> namesToCheck, IReadOnlyList<string> referenceNames)
    {
        var referenceSet = new HashSet<string>(referenceNames, StringComparer.OrdinalIgnoreCase);

        return namesToCheck.Where(name => !referenceSet.Contains(name)).ToList();
    }
}
