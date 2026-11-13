using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads the free space of a volume (#1400).</summary>
public interface IDiskSpaceProbe
{
    /// <summary>Reads the bytes available to the user on the volume that holds the folder.</summary>
    /// <param name="folder">A folder on the volume; it need not exist.</param>
    /// <returns>The free bytes, or none when the volume cannot be read, in which case the caller carries on.</returns>
    Option<long> GetAvailableBytes(IDirectoryInfo folder);
}
