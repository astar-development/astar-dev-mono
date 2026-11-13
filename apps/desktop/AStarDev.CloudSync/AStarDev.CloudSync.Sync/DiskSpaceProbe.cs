using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
internal sealed class DiskSpaceProbe : IDiskSpaceProbe
{
    /// <inheritdoc />
    public Option<long> GetAvailableBytes(IDirectoryInfo folder) =>
        Try.Run(() => folder.FileSystem.DriveInfo.New(folder.FullName).AvailableFreeSpace).Match(Option.Some, _ => Option.None<long>());
}
