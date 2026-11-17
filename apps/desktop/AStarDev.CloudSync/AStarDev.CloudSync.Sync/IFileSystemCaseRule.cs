namespace AStarDev.CloudSync.Sync;

/// <summary>Supplies the case rule of the local file system: two local paths that differ only in casing name the same folder when the rule ignores case.</summary>
public interface IFileSystemCaseRule
{
    /// <summary>The comparison that tells whether two full local paths name the same entry: <see cref="StringComparison.OrdinalIgnoreCase"/> on a case-insensitive file system, otherwise <see cref="StringComparison.Ordinal"/>.</summary>
    StringComparison Comparison { get; }
}
