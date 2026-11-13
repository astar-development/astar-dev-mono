using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class FakeDiskSpaceProbe : IDiskSpaceProbe
{
    public Option<long> Free { get; set; } = Option.Some(long.MaxValue);

    public Option<long> GetAvailableBytes(IDirectoryInfo folder) => Free;
}
