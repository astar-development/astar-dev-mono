using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Supplies the current user's home folder.</summary>
public interface IHomeFolder
{
    /// <summary>The user's home folder, or none when the platform does not report one.</summary>
    Option<DirectoryInfo> Folder { get; }
}
