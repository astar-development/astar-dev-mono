using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class LocalFileFilter : ILocalFileFilter
{
    private const char DotPrefix = '.';
    private const string OfficeLockPrefix = "~$";

    private static readonly string[] SystemFileNames = ["desktop.ini", "Thumbs.db"];

    /// <inheritdoc />
    public LocalFileVisibility ShouldSkip(IFileInfo file)
    {
        if (IsLink(file))
            return Skipped(FileSkipReasonFactory.CreateSymbolicLink());

        if (IsHidden(file))
            return Skipped(FileSkipReasonFactory.CreateHidden());

        if (IsAppTemporary(file.Name))
            return Skipped(FileSkipReasonFactory.CreateTemporary());

        if (file.Name.StartsWith(OfficeLockPrefix, StringComparison.Ordinal))
            return Skipped(FileSkipReasonFactory.CreateOfficeLock());

        return SystemFileNames.Contains(file.Name, StringComparer.OrdinalIgnoreCase) ? Skipped(FileSkipReasonFactory.CreateSystemFile()) : LocalFileVisibilityFactory.CreateVisible();
    }

    private static LocalFileVisibility Skipped(FileSkipReason reason) => LocalFileVisibilityFactory.CreateSkipped(reason);

    private static bool IsAppTemporary(string name) => RelativeSyncPathFactory.Create(name).Match(SyncSkipFilter.IsSkipped, _ => false);

    private static bool IsLink(IFileInfo file) => Reads(() => file.Exists && (file.Attributes.HasFlag(FileAttributes.ReparsePoint) || file.LinkTarget is not null));

    private static bool IsHidden(IFileInfo file) => !file.Name.StartsWith(DotPrefix) && Reads(() => file.Exists && file.Attributes.HasFlag(FileAttributes.Hidden));

    private static bool Reads(Func<bool> read) => Try.Run(read).Match(value => value, _ => false);
}
