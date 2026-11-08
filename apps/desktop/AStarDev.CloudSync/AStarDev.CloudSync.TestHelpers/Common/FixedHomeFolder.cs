using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Common;

public sealed class FixedHomeFolder(Option<DirectoryInfo> folder) : IHomeFolder
{
    public Option<DirectoryInfo> Folder => folder;
}
