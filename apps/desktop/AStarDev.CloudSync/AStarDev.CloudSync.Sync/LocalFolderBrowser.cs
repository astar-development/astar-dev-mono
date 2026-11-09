using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="syncRoots">Finds the account's sync root.</param>
/// <param name="filter">Decides which folders are left out.</param>
/// <param name="logger">Reports a child folder that cannot be read, without its name.</param>
public sealed class LocalFolderBrowser(ISyncRootReader syncRoots, ILocalFolderFilter filter, ILogger<LocalFolderBrowser> logger) : ILocalFolderBrowser
{
    private const string Location = nameof(LocalFolderBrowser);
    private const char Separator = '/';

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<LocalFolder>, LocalFolderError>> GetChildFoldersAsync(AccountId accountId, LocalRelativePath parent, CancellationToken cancellationToken) =>
        syncRoots.GetAsync(accountId, cancellationToken)
            .MatchAsync(
                root => root.Match(found => Task.Run(() => List(found, parent, cancellationToken), cancellationToken), () => Task.FromResult(Failed(LocalFolderErrorFactory.CreateRootUnavailable()))),
                error => Task.FromResult(Failed(LocalFolderErrorFactory.CreateUnexpected(error))));

    private static Result<IReadOnlyList<LocalFolder>, LocalFolderError> Failed(LocalFolderError error) => Result.Failure<IReadOnlyList<LocalFolder>, LocalFolderError>(error);

    private static LocalRelativePath ChildPath(LocalRelativePath parent, string name) =>
        LocalRelativePathFactory.Create(string.IsNullOrEmpty(parent.Value) ? name : parent.Value + Separator + name).Match(path => path, _ => parent);

    private Result<IReadOnlyList<LocalFolder>, LocalFolderError> List(SyncRoot root, LocalRelativePath parent, CancellationToken cancellationToken)
    {
        root.Directory.Refresh();
        if (!root.Directory.Exists)
            return Failed(LocalFolderErrorFactory.CreateRootUnavailable());

        var fileSystem = root.Directory.FileSystem;
        var parentDirectory = fileSystem.DirectoryInfo.New(fileSystem.Path.Join(root.Directory.FullName, parent.Value));
        if (!parentDirectory.Exists)
            return Failed(LocalFolderErrorFactory.CreateNotFound());

        try
        {
            return Result.Success<IReadOnlyList<LocalFolder>, LocalFolderError>([.. ReadChildren(parentDirectory, parent, cancellationToken).OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)]);
        }
        catch (UnauthorizedAccessException)
        {
            return Failed(LocalFolderErrorFactory.CreateAccessDenied());
        }
        catch (DirectoryNotFoundException)
        {
            return Failed(LocalFolderErrorFactory.CreateNotFound());
        }
        catch (IOException)
        {
            return Failed(LocalFolderErrorFactory.CreateUnexpected(new ErrorResponse("The folders could not be read.")));
        }
    }

    private IEnumerable<LocalFolder> ReadChildren(IDirectoryInfo parentDirectory, LocalRelativePath parent, CancellationToken cancellationToken)
    {
        foreach (var child in parentDirectory.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (filter.ShouldSkip(child) is not LocalFolderVisibility.Visible)
                continue;

            foreach (var children in ChildrenOf(child))
                yield return LocalFolderFactory.Create(child.Name, ChildPath(parent, child.Name), children);
        }
    }

    private IEnumerable<LocalFolderChildren> ChildrenOf(IDirectoryInfo child)
    {
        try
        {
            child.Refresh();
            if (!child.Exists)
                return [];

            return [child.EnumerateDirectories().Any(grandChild => filter.ShouldSkip(grandChild) is LocalFolderVisibility.Visible) ? LocalFolderChildrenFactory.CreateHasChildFolders() : LocalFolderChildrenFactory.CreateNoChildFolders()];
        }
        catch (DirectoryNotFoundException)
        {
            return [];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogMessage.Warning(logger, Location, "A child folder could not be read, so it is listed without its sub-folders.");

            return [LocalFolderChildrenFactory.CreateNoChildFolders()];
        }
    }
}
