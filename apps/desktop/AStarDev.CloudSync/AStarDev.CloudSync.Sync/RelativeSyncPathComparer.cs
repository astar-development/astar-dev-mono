namespace AStarDev.CloudSync.Sync;

/// <summary>Compares <see cref="RelativeSyncPath"/> values ignoring case, as OneDrive does.</summary>
public sealed class RelativeSyncPathComparer : IEqualityComparer<RelativeSyncPath>
{
    private RelativeSyncPathComparer()
    {
    }

    /// <summary>The shared comparer.</summary>
    public static RelativeSyncPathComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(RelativeSyncPath x, RelativeSyncPath y) => string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public int GetHashCode(RelativeSyncPath obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Value ?? string.Empty);
}
