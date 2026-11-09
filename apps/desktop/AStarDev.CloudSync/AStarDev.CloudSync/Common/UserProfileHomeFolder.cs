using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Common;

/// <summary>The home folder reported by the platform for <see cref="Environment.SpecialFolder.UserProfile"/>.</summary>
/// <param name="fileSystem">Creates the folder abstraction for the reported path.</param>
[ExcludeFromCodeCoverage]
public sealed class UserProfileHomeFolder(IFileSystem fileSystem) : IHomeFolder
{
    /// <inheritdoc />
    public Option<IDirectoryInfo> Folder
    {
        get
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return string.IsNullOrWhiteSpace(path) ? Option.None<IDirectoryInfo>() : Option.Some(fileSystem.DirectoryInfo.New(path));
        }
    }
}
