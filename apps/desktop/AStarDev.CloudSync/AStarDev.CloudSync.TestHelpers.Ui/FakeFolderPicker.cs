using System.IO.Abstractions;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Ui;

public sealed class FakeFolderPicker : IFolderPicker
{
    public Option<IDirectoryInfo> Choice { get; set; } = Option.None<IDirectoryInfo>();

    public int Calls { get; private set; }

    public Task<Option<IDirectoryInfo>> PickAsync(Option<IDirectoryInfo> startIn, CancellationToken cancellationToken)
    {
        Calls++;

        return Task.FromResult(Choice);
    }
}
