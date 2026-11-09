using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class LocalFolderFilter : ILocalFolderFilter
{
    private const char DotPrefix = '.';

    /// <inheritdoc />
    public LocalFolderVisibility ShouldSkip(IDirectoryInfo folder)
    {
        if (folder.Name.StartsWith(DotPrefix))
            return Skipped(SkipReasonFactory.CreateDotPrefixed());

        if (HasAttribute(folder, FileAttributes.Hidden))
            return Skipped(SkipReasonFactory.CreateHidden());

        if (IsLink(folder))
            return Skipped(SkipReasonFactory.CreateSymbolicLink());

        return IsAppTemporary(folder.Name) ? Skipped(SkipReasonFactory.CreateAppTemporary()) : LocalFolderVisibilityFactory.CreateVisible();
    }

    private static LocalFolderVisibility Skipped(SkipReason reason) => LocalFolderVisibilityFactory.CreateSkipped(reason);

    private static bool IsAppTemporary(string name) => RelativeSyncPathFactory.Create(name).Match(SyncSkipFilter.IsSkipped, _ => false);

    private static bool IsLink(IDirectoryInfo folder) => HasAttribute(folder, FileAttributes.ReparsePoint) || Reads(() => folder.LinkTarget is not null);

    private static bool HasAttribute(IDirectoryInfo folder, FileAttributes attribute) => Reads(() => folder.Attributes.HasFlag(attribute));

    private static bool Reads(Func<bool> read)
    {
        try
        {
            return read();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
