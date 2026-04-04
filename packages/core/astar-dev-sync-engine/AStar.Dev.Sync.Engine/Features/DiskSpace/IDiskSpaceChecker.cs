namespace AStar.Dev.Sync.Engine.Features.DiskSpace;

/// <summary>Abstracts disk space queries so implementations can be replaced in tests (EH-03).</summary>
public interface IDiskSpaceChecker
{
    /// <summary>Returns the available free bytes on the drive that hosts <paramref name="path"/>.</summary>
    long GetAvailableFreeSpace(string path);
}
