using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.ViewModels.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Common;

/// <summary>The home folder reported by the platform for <see cref="Environment.SpecialFolder.UserProfile"/>.</summary>
[ExcludeFromCodeCoverage]
public sealed class UserProfileHomeFolder : IHomeFolder
{
    /// <inheritdoc />
    public Option<DirectoryInfo> Folder
    {
        get
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return string.IsNullOrWhiteSpace(path) ? Option.None<DirectoryInfo>() : Option.Some(new DirectoryInfo(path));
        }
    }
}
