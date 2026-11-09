using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class TestHomeFolder(Option<IDirectoryInfo> folder) : IHomeFolder
{
    public Option<IDirectoryInfo> Folder => folder;
}
