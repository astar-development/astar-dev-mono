namespace AStarDev.CloudSync.Sync;

/// <summary>Compares <see cref="RelativeSyncPath"/> values ignoring case, as OneDrive does. The same as the type's own equality; use it where an explicit comparer documents intent.</summary>
public sealed class RelativeSyncPathComparer : IEqualityComparer<RelativeSyncPath>
{
    private RelativeSyncPathComparer()
    {
    }

    /// <summary>The shared comparer.</summary>
    public static RelativeSyncPathComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(RelativeSyncPath x, RelativeSyncPath y) => x.Equals(y);

    /// <inheritdoc />
    public int GetHashCode(RelativeSyncPath obj) => obj.GetHashCode();
}
