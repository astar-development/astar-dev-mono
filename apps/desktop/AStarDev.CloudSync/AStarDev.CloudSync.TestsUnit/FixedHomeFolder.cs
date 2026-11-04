using AStarDev.CloudSync.Core;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit;

internal sealed class FixedHomeFolder(Option<DirectoryInfo> folder) : IHomeFolder
{
    public Option<DirectoryInfo> Folder => folder;
}
