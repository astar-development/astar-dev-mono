using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Ui;

public sealed class FixedHomeFolder(Option<DirectoryInfo> folder) : IHomeFolder
{
    public Option<DirectoryInfo> Folder => folder;
}
