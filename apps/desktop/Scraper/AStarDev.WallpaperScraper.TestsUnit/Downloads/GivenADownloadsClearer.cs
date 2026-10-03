using AStarDev.WallpaperScraper.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.Downloads;
using AStarDev.WallpaperScraper.TestsUnit.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions.Testing;
using Testably.Abstractions.Testing.FileSystem;

namespace AStarDev.WallpaperScraper.TestsUnit.Downloads;

public sealed class GivenADownloadsClearer : IDisposable
{
    private readonly MockFileSystem fileSystem = new();
    private readonly FakeFileDetailsClearer fileDetailsClearer = new();
    private readonly FakeScrapeConfigurationLookup lookup = new();
    private readonly ServiceProvider serviceProvider;
    private readonly DownloadsClearer clearer;

    public GivenADownloadsClearer()
    {
        lookup.RootDirectoriesResult = Exceptional.Success(Option.Some(new RootDirectories("/scrapes/root", "/scrapes/famous")));
        serviceProvider = new ServiceCollection()
            .AddScoped<IScrapeConfigurationLookup>(_ => lookup)
            .AddScoped<IFileDetailsClearer>(_ => fileDetailsClearer)
            .BuildServiceProvider();
        clearer = new DownloadsClearer(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), fileSystem);
    }

    [Fact]
    public async Task when_downloads_exist_then_the_file_records_are_cleared_and_both_directories_are_emptied_but_kept()
    {
        fileDetailsClearer.Count = 3;
        await AddFileAsync("/scrapes/root/top-wallpapers/one.jpg", "one");
        await AddFileAsync("/scrapes/famous/emma-stone/hot-wallpapers/two.jpg", "two");

        var result = await clearer.ClearAsync(TestContext.Current.CancellationToken);

        (result.Match(cleared => cleared.FileRecords, exception => throw exception),
            fileSystem.Directory.GetFileSystemEntries("/scrapes/root").Length,
            fileSystem.Directory.GetFileSystemEntries("/scrapes/famous").Length,
            fileSystem.Directory.Exists("/scrapes/root"),
            fileSystem.Directory.Exists("/scrapes/famous"))
            .ShouldBe((3, 0, 0, true, true));
    }

    [Fact]
    public async Task when_files_exist_outside_the_two_directories_then_they_are_left_alone()
    {
        await AddFileAsync("/scrapes/root/one.jpg", "one");
        await AddFileAsync("/scrapes/other/keep.jpg", "keep");

        _ = await clearer.ClearAsync(TestContext.Current.CancellationToken);

        fileSystem.File.Exists("/scrapes/other/keep.jpg").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_famous_directory_is_inside_the_root_then_both_directories_exist_afterwards()
    {
        lookup.RootDirectoriesResult = Exceptional.Success(Option.Some(new RootDirectories("/scrapes/root", "/scrapes/root/famous")));
        await AddFileAsync("/scrapes/root/famous/one.jpg", "one");

        _ = await clearer.ClearAsync(TestContext.Current.CancellationToken);

        (fileSystem.Directory.Exists("/scrapes/root"), fileSystem.Directory.Exists("/scrapes/root/famous"), fileSystem.Directory.GetFileSystemEntries("/scrapes/root/famous").Length).ShouldBe((true, true, 0));
    }

    [Fact]
    public async Task when_the_file_records_cannot_be_cleared_then_the_failure_is_returned_and_the_files_are_left_alone()
    {
        var failure = new InvalidOperationException("clear failed");
        fileDetailsClearer.Failure = Option.Some<Exception>(failure);
        await AddFileAsync("/scrapes/root/one.jpg", "one");

        var result = await clearer.ClearAsync(TestContext.Current.CancellationToken);

        (result.Match(_ => (Exception?)null, exception => exception), fileSystem.File.Exists("/scrapes/root/one.jpg")).ShouldBe((failure, true));
    }

    [Fact]
    public async Task when_the_directories_cannot_be_emptied_then_the_failure_reports_that_the_file_records_were_already_cleared()
    {
        fileDetailsClearer.Count = 3;
        await AddFileAsync("/scrapes/root/locked.jpg", "locked");
        _ = fileSystem.Intercept.Deleting(FileSystemTypes.File, _ => throw new IOException("The file is in use."));

        var result = await clearer.ClearAsync(TestContext.Current.CancellationToken);

        var failure = result.Match(_ => (Exception?)null, exception => exception).ShouldNotBeNull();
        (failure.Message.Contains("File records cleared: 3", StringComparison.Ordinal), failure.InnerException is IOException).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_there_is_no_scrape_configuration_then_a_failure_is_returned_and_nothing_is_cleared()
    {
        lookup.RootDirectoriesResult = Exceptional.Success(Option.None<RootDirectories>());
        fileDetailsClearer.Count = 3;

        var result = await clearer.ClearAsync(TestContext.Current.CancellationToken);

        (result.Match(_ => false, _ => true), fileDetailsClearer.ClearCount).ShouldBe((true, 0));
    }

    public void Dispose() => serviceProvider.Dispose();

    private async Task AddFileAsync(string path, string contents)
    {
        _ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        await fileSystem.File.WriteAllTextAsync(path, contents, TestContext.Current.CancellationToken);
    }

    private sealed class FakeFileDetailsClearer : IFileDetailsClearer
    {
        public int Count { get; set; }

        public Option<Exception> Failure { get; set; } = Option.None<Exception>();

        public int ClearCount { get; private set; }

        public Task<Exceptional<int>> ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;

            return Task.FromResult(Failure.Match(failure => (Exceptional<int>)failure, () => Exceptional.Success(Count)));
        }
    }
}
