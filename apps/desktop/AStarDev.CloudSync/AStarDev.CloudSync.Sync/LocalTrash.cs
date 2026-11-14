using System.Globalization;
using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Moves items into the <c>.sync-trash</c> folder of a sync root and removes the entries that outlived the retention (#1583). Each move gets its own <c>&lt;utc stamp&gt;-&lt;guid&gt;</c> folder that keeps the item's relative path, so no earlier copy is ever overwritten and an entry's age is in its name.</summary>
/// <param name="timeProvider">Stamps trashed items and decides which have expired.</param>
/// <param name="options">The retention.</param>
internal sealed class LocalTrash(TimeProvider timeProvider, LocalTrashOptions options)
{
    internal const string FolderName = ".sync-trash";

    private const string StampFormat = "yyyyMMddHHmmssfff";
    private const char StampSeparator = '-';
    private const string ParentSegment = "..";

    internal static bool Contains(RelativeSyncPath path) => path.Value.Split('/')[0].Equals(FolderName, StringComparison.OrdinalIgnoreCase);

    internal void Move(IDirectoryInfo root, IFileSystemInfo item)
    {
        item.Refresh();
        if (!item.Exists)
            return;

        var path = root.FileSystem.Path;
        var relative = path.GetRelativePath(root.FullName, item.FullName);
        var first = FirstSegment(relative, path);
        if (first is "." or ParentSegment || path.IsPathRooted(relative))
            throw new InvalidOperationException("The item is not inside the sync root.");

        if (first.Equals(FolderName, StringComparison.OrdinalIgnoreCase))
            return;

        var entry = $"{timeProvider.GetUtcNow().UtcDateTime.ToString(StampFormat, CultureInfo.InvariantCulture)}{StampSeparator}{Guid.NewGuid():N}";
        LocalFileOperations.MoveItem(item, path.Join(root.FullName, FolderName, entry, relative));
    }

    internal int Purge(IDirectoryInfo root) => Try.Run(() => PurgeExpired(root)).Match(count => count, _ => 0);

    private static string FirstSegment(string relative, IPath path) => relative.Split([path.DirectorySeparatorChar, path.AltDirectorySeparatorChar])[0];

    private static bool DeleteQuietly(IDirectoryInfo entry) => Try.Run(() =>
    {
        entry.Delete(recursive: true);

        return true;
    }).Match(deleted => deleted, _ => false);

    private static bool IsOlderThan(string entryName, DateTimeOffset cutoff) =>
        DateTimeOffset.TryParseExact(entryName.Split(StampSeparator)[0], StampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var trashedAt) && trashedAt <= cutoff;

    private int PurgeExpired(IDirectoryInfo root)
    {
        var trash = root.FileSystem.DirectoryInfo.New(root.FileSystem.Path.Join(root.FullName, FolderName));
        if (!trash.Exists)
            return 0;

        var cutoff = timeProvider.GetUtcNow() - options.Retention;

        IDirectoryInfo[] expired = [.. trash.EnumerateDirectories().Where(entry => IsOlderThan(entry.Name, cutoff))];

        return expired.Count(DeleteQuietly);
    }
}
