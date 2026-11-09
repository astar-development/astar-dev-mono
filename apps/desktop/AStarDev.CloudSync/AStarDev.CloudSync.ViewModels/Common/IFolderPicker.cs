using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Lets the user choose a folder.</summary>
public interface IFolderPicker
{
    /// <summary>Shows the folder chooser.</summary>
    /// <param name="startIn">The folder the chooser opens in, when there is one.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The chosen folder, or none when the user cancelled.</returns>
    Task<Option<IDirectoryInfo>> PickAsync(Option<IDirectoryInfo> startIn, CancellationToken cancellationToken);
}
