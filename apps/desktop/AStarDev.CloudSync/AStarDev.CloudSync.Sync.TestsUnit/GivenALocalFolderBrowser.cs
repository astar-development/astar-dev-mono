using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALocalFolderBrowser
{
    private const string Root = "/home/jbarden/CloudSync/OneDrive-Personal";
    private static readonly AccountId Account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly FakeSyncRootReader syncRoots = new();
    private readonly RecordingLogger<LocalFolderBrowser> logger = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_root_is_listed_then_its_child_folders_are_returned_sorted_ignoring_case_and_without_files()
    {
        Prepare("Zebra", "apple", "Banana");
        fileSystem.File.WriteAllText($"{Root}/notes.txt", "x");

        var result = await List(LocalRelativePathFactory.Root);

        Names(result).ShouldBe(["apple", "Banana", "Zebra"]);
    }

    [Fact]
    public async Task when_a_sub_folder_is_listed_then_its_children_have_paths_relative_to_the_root()
    {
        Prepare("Photos/2025", "Photos/2026");

        var result = await List(Path("Photos"));

        result.Match(folders => folders.Select(folder => folder.Path.Value), _ => []).ShouldBe(["Photos/2025", "Photos/2026"]);
    }

    [Fact]
    public async Task when_a_folder_has_a_visible_sub_folder_then_it_has_child_folders_and_otherwise_it_has_none()
    {
        Prepare("WithChild/Inner", "Empty");
        fileSystem.File.WriteAllText($"{Root}/Empty/file.txt", "x");

        var result = await List(LocalRelativePathFactory.Root);

        result.Match(folders => folders.Select(folder => (folder.Name, folder.Children)), _ => []).ShouldBe([("Empty", LocalFolderChildrenFactory.CreateNoChildFolders()), ("WithChild", LocalFolderChildrenFactory.CreateHasChildFolders())]);
    }

    [Fact]
    public async Task when_a_folder_only_has_skipped_sub_folders_then_it_has_no_child_folders()
    {
        Prepare("OnlyDot/.hidden");

        var result = await List(LocalRelativePathFactory.Root);

        result.Match(folders => folders.Single().Children, _ => LocalFolderChildrenFactory.CreateHasChildFolders()).ShouldBe(LocalFolderChildrenFactory.CreateNoChildFolders());
    }

    [Fact]
    public async Task when_a_child_folder_is_skipped_by_the_filter_then_it_is_not_listed()
    {
        Prepare("Visible", ".dot", "report.3f2a9c1e-5b7d-4e6a-8c0d-1a2b3c4d5e6f.download");

        var result = await List(LocalRelativePathFactory.Root);

        Names(result).ShouldBe(["Visible"]);
    }

    [Fact]
    public async Task when_a_child_folder_is_a_symbolic_link_then_it_is_not_listed_and_not_followed()
    {
        Prepare("Real");
        _ = fileSystem.Directory.CreateDirectory("/elsewhere/Secret");
        _ = fileSystem.Directory.CreateSymbolicLink($"{Root}/Link", "/elsewhere");

        var result = await List(LocalRelativePathFactory.Root);

        Names(result).ShouldBe(["Real"]);
    }

    [Fact]
    public async Task when_the_account_has_no_sync_root_then_the_root_is_unavailable()
    {
        var result = await Browser().GetChildFoldersAsync(Account, LocalRelativePathFactory.Root, CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(LocalFolderErrorFactory.CreateRootUnavailable());
    }

    [Fact]
    public async Task when_the_root_folder_is_missing_then_the_root_is_unavailable()
    {
        SetRoot();

        var result = await List(LocalRelativePathFactory.Root);

        result.Match(_ => (object)"listed", error => error).ShouldBe(LocalFolderErrorFactory.CreateRootUnavailable());
    }

    [Fact]
    public async Task when_the_sync_roots_cannot_be_read_then_the_listing_is_unexpected_rather_than_empty()
    {
        syncRoots.Fails = true;

        var result = await Browser().GetChildFoldersAsync(Account, LocalRelativePathFactory.Root, CancellationToken);

        result.Match(_ => false, error => error is LocalFolderError.Unexpected).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_parent_folder_does_not_exist_then_it_is_not_found()
    {
        Prepare("Photos");

        var result = await List(Path("Missing"));

        result.Match(_ => (object)"listed", error => error).ShouldBe(LocalFolderErrorFactory.CreateNotFound());
    }

    [Fact]
    public async Task when_the_parent_folder_cannot_be_read_then_access_is_denied()
    {
        Prepare("Locked/Inner");
        fileSystem.WithAccessControlStrategy(new DenyReading($"{Root}/Locked"));

        var result = await List(Path("Locked"));

        result.Match(_ => (object)"listed", error => error).ShouldBe(LocalFolderErrorFactory.CreateAccessDenied());
    }

    [Fact]
    public async Task when_a_child_folder_cannot_be_read_then_it_is_still_listed_without_sub_folders_and_a_warning_is_logged_without_its_name()
    {
        Prepare("Open/Inner", "Locked/Inner");
        fileSystem.WithAccessControlStrategy(new DenyReading($"{Root}/Locked"));

        var result = await List(LocalRelativePathFactory.Root);

        result.Match(folders => folders.Select(folder => (folder.Name, folder.Children)), _ => []).ShouldBe([("Locked", LocalFolderChildrenFactory.CreateNoChildFolders()), ("Open", LocalFolderChildrenFactory.CreateHasChildFolders())]);
        logger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldBe(1);
        logger.Messages.ShouldNotContain(message => message.Contains("Locked", StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_a_child_folder_is_deleted_while_the_listing_runs_then_it_is_skipped_rather_than_failing_the_listing()
    {
        Prepare("a", "gone", "z");
        var filter = new DeletingFilter(fileSystem, $"{Root}/gone");

        var result = await Browser(filter).GetChildFoldersAsync(Account, LocalRelativePathFactory.Root, CancellationToken);

        Names(result).ShouldBe(["a", "z"]);
    }

    [Fact]
    public async Task when_a_folder_has_ten_thousand_child_folders_then_all_are_listed_sorted_and_off_the_calling_thread()
    {
        SetRoot();
        foreach (var number in Enumerable.Range(0, 10_000))
            _ = fileSystem.Directory.CreateDirectory($"{Root}/folder-{number:00000}");
        var filter = new ThreadRecordingFilter();

        var result = await Browser(filter).GetChildFoldersAsync(Account, LocalRelativePathFactory.Root, CancellationToken);

        var names = Names(result);
        (names.Count, names[0], names[^1], filter.Threads.Contains(Environment.CurrentManagedThreadId)).ShouldBe((10_000, "folder-00000", "folder-09999", false));
    }

    [Fact]
    public async Task when_the_token_is_cancelled_then_the_listing_is_cancelled()
    {
        Prepare("a", "b");
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var listing = Browser().GetChildFoldersAsync(Account, LocalRelativePathFactory.Root, cancelled.Token);

        await Should.ThrowAsync<OperationCanceledException>(listing);
    }

    private static List<string> Names(Result<IReadOnlyList<LocalFolder>, LocalFolderError> result) => [.. result.Match(folders => folders.Select(folder => folder.Name), _ => [])];

    private static LocalRelativePath Path(string value) => LocalRelativePathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private void SetRoot() =>
        syncRoots.Roots = [AccountSyncRootFactory.Create(Account, SyncRootFactory.Create(fileSystem.DirectoryInfo.New(Root)).Match(root => root, _ => throw new InvalidOperationException("The test root must be valid.")))];

    private void Prepare(params string[] folders)
    {
        _ = fileSystem.Directory.CreateDirectory(Root);
        foreach (var folder in folders)
            _ = fileSystem.Directory.CreateDirectory($"{Root}/{folder}");

        SetRoot();
    }

    private Task<Result<IReadOnlyList<LocalFolder>, LocalFolderError>> List(LocalRelativePath parent) => Browser().GetChildFoldersAsync(Account, parent, CancellationToken);

    private LocalFolderBrowser Browser(ILocalFolderFilter? filter = null) => new(syncRoots, filter ?? new LocalFolderFilter(), logger);

    private sealed class DenyReading(string deniedPath) : Testably.Abstractions.Testing.FileSystem.IAccessControlStrategy
    {
        public bool IsAccessGranted(string fullPath, Testably.Abstractions.Helpers.IFileSystemExtensibility extensibility) => !string.Equals(fullPath.TrimEnd('/'), deniedPath, StringComparison.Ordinal);
    }

    private sealed class DeletingFilter(MockFileSystem fileSystem, string pathToDelete) : ILocalFolderFilter
    {
        public LocalFolderVisibility ShouldSkip(IDirectoryInfo folder)
        {
            if (fileSystem.Directory.Exists(pathToDelete))
                fileSystem.Directory.Delete(pathToDelete, recursive: true);

            return LocalFolderVisibilityFactory.CreateVisible();
        }
    }

    private sealed class ThreadRecordingFilter : ILocalFolderFilter
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<int> threads = [];

        public IReadOnlyCollection<int> Threads => threads;

        public LocalFolderVisibility ShouldSkip(IDirectoryInfo folder)
        {
            threads.Add(Environment.CurrentManagedThreadId);

            return LocalFolderVisibilityFactory.CreateVisible();
        }
    }
}
