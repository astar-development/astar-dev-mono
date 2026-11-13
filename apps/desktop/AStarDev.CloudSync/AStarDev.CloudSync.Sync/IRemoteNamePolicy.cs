using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A provider's rules for the names, path lengths and file sizes it accepts. Registered per provider.</summary>
public interface IRemoteNamePolicy
{
    /// <summary>Checks an item before it is uploaded.</summary>
    /// <param name="name">The item's own name (the last segment of the path).</param>
    /// <param name="path">The item's path relative to the sync root.</param>
    /// <param name="size">The size in bytes; zero for a folder.</param>
    /// <returns>Success, or why the provider would reject the item.</returns>
    Result<Unit, InvalidRemoteName> Check(string name, RelativeSyncPath path, long size);
}
