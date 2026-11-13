using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class ArmingFolderFilter(ILocalFolderFilter inner, DenyListing denial) : ILocalFolderFilter
{
    public LocalFolderVisibility ShouldSkip(IDirectoryInfo folder)
    {
        var visibility = inner.ShouldSkip(folder);
        denial.ArmWhenSeen(folder.FullName);

        return visibility;
    }
}
