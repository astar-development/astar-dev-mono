using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.Sqlite.TestsIntegration;

public sealed class TestHomeFolder(IDirectoryInfo folder) : IHomeFolder
{
    public Option<IDirectoryInfo> Folder => Option.Some(folder);
}
