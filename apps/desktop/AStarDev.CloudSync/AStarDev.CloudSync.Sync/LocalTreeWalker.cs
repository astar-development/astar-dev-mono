using AStarDev.FunctionalParadigm;
using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <summary>Walks an account's sync root once, going only into folders the selection rules include or that have an included folder below them (#1399). Each path is reached once: of two siblings that differ only in case, the first by ordinal name is walked and the other is reported as clashing.</summary>
/// <param name="evaluator">Judges folders against the rules.</param>
/// <param name="folderFilter">Skips hidden, dot-prefixed, linked and app temporary folders.</param>
/// <param name="fileFilter">Skips hidden, linked, temporary, lock and system files.</param>
internal sealed class LocalTreeWalker(ISyncSelectionEvaluator evaluator, ILocalFolderFilter folderFilter, ILocalFileFilter fileFilter)
{
    private const char Separator = '/';

    internal IEnumerable<LocalWalkItem> Walk(IDirectoryInfo root, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken) =>
        Descends(LocalRelativePathFactory.Root, rules) ? WalkFolder(root, string.Empty, rules, cancellationToken) : [];

    private static string Join(string parent, string name) => parent.Length == 0 ? name : $"{parent}{Separator}{name}";

    private static bool TryPaths(string path, out LocalRelativePath local, out RelativeSyncPath sync)
    {
        var localResult = LocalRelativePathFactory.Create(path);
        var syncResult = RelativeSyncPathFactory.Create(path);
        local = localResult.Match(valid => valid, _ => default);
        sync = syncResult.Match(valid => valid, _ => default);

        return localResult.Match(_ => true, _ => false) && syncResult.Match(_ => true, _ => false);
    }

    private IEnumerable<LocalWalkItem> WalkFolder(IDirectoryInfo folder, string path, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        var includesFiles = TryPaths(path, out var folderPath, out _) && evaluator.IsIncluded(folderPath, rules);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var listing = Try.Run(() => folder.EnumerateFileSystemInfos().OrderBy(info => info.Name, StringComparer.Ordinal).ToList());
        var children = listing.Match(found => found, _ => (List<IFileSystemInfo>?)null);
        if (children is null)
        {
            if (path.Length == 0)
                throw new IOException("The sync root could not be listed.");

            if (TryPaths(path, out _, out var unlistable))
                yield return LocalWalkItem.Unlistable(unlistable);

            yield break;
        }

        foreach (var child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var item in Visit(child, Join(path, child.Name), includesFiles, seen, rules, cancellationToken))
                yield return item;
        }
    }

    private IEnumerable<LocalWalkItem> Visit(IFileSystemInfo child, string childPath, bool includesFiles, HashSet<string> seen, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        if (!TryPaths(childPath, out var local, out var sync))
            yield break;

        if (!IsWanted(child, local, includesFiles, rules))
            yield break;

        if (!seen.Add(child.Name))
        {
            yield return LocalWalkItem.Clashing(sync);

            yield break;
        }

        if (child is not IDirectoryInfo subfolder)
        {
            yield return LocalWalkItem.Found(sync, child);

            yield break;
        }

        if (includesFiles && evaluator.IsIncluded(local, rules))
            yield return LocalWalkItem.Found(sync, child);

        foreach (var item in WalkFolder(subfolder, childPath, rules, cancellationToken))
            yield return item;
    }

    private bool Descends(LocalRelativePath folder, IReadOnlyList<SelectionRule> rules) => evaluator.IsIncluded(folder, rules) || evaluator.Evaluate(folder, rules) is SelectionState.PartiallyIncluded;

    private bool IsWanted(IFileSystemInfo child, LocalRelativePath path, bool includesFiles, IReadOnlyList<SelectionRule> rules) => child switch
    {
        IDirectoryInfo directory => Descends(path, rules) && folderFilter.ShouldSkip(directory) is LocalFolderVisibility.Visible,
        IFileInfo file => includesFiles && fileFilter.ShouldSkip(file) is LocalFileVisibility.Visible,
        _ => false,
    };
}
