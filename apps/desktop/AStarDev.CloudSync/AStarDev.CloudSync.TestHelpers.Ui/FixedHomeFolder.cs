using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Ui;

public sealed class FixedHomeFolder(Option<IDirectoryInfo> folder) : IHomeFolder
{
    public Option<IDirectoryInfo> Folder => folder;
}
