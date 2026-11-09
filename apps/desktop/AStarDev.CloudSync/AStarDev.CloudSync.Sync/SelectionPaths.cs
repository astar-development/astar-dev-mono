namespace AStarDev.CloudSync.Sync;

internal static class SelectionPaths
{
    private const char Separator = '/';

    internal static string[] Segments(LocalRelativePath path) => (path.Value ?? string.Empty).Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    internal static bool IsAncestorOrSelf(LocalRelativePath ancestor, LocalRelativePath descendant)
    {
        var ancestorSegments = Segments(ancestor);
        var descendantSegments = Segments(descendant);

        return ancestorSegments.Length <= descendantSegments.Length && ancestorSegments.Zip(descendantSegments).All(pair => string.Equals(pair.First, pair.Second, StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsStrictlyBelow(LocalRelativePath candidate, LocalRelativePath ancestor) => IsAncestorOrSelf(ancestor, candidate) && Segments(candidate).Length > Segments(ancestor).Length;

    internal static int Depth(LocalRelativePath path) => Segments(path).Length;
}
