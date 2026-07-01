using AStar.Dev.Wallpaper.Scrapper.Repositories;
using AStar.Dev.Wallpaper.Scrapper.Services;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Services;

public sealed class GivenADatabaseResetService
{
    private const string SaveDirectory = "/some/save/dir";
    private const string NonExistentDirectory = "/nonexistent/path";

    private readonly IDatabaseResetRepository repo = Substitute.For<IDatabaseResetRepository>();
    private readonly MockFileSystem fileSystem = new();
    private readonly DatabaseResetService sut;

    public GivenADatabaseResetService() => sut = new DatabaseResetService(repo, fileSystem);

    [Fact]
    public async Task when_resetting_then_reset_search_categories_is_called()
    {
        await sut.ResetAsync(CancellationToken.None);

        await repo.Received(1).ResetSearchCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resetting_then_delete_all_files_is_called()
    {
        await sut.ResetAsync(CancellationToken.None);

        await repo.Received(1).DeleteAllFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_resetting_then_reset_search_categories_is_called_before_delete_all_files()
    {
        var callOrder = new List<string>();
        repo.When(r => r.ResetSearchCategoriesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("categories"));
        repo.When(r => r.DeleteAllFilesAsync(Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("files"));

        await sut.ResetAsync(CancellationToken.None);

        callOrder.ShouldBe(["categories", "files"]);
    }

    [Fact]
    public async Task when_reset_search_categories_throws_then_delete_all_files_is_not_called()
    {
        repo.ResetSearchCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException()));

        await Should.ThrowAsync<InvalidOperationException>(() => sut.ResetAsync(CancellationToken.None));

        await repo.DidNotReceive().DeleteAllFilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_deleting_save_directory_then_get_base_save_directory_is_called()
    {
        await sut.DeleteSaveDirectoryAsync(CancellationToken.None);

        await repo.Received(1).GetBaseSaveDirectoryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_deleting_save_directory_and_directory_exists_then_directory_is_deleted()
    {
        repo.GetBaseSaveDirectoryAsync(Arg.Any<CancellationToken>()).Returns(SaveDirectory);
        fileSystem.Directory.CreateDirectory(SaveDirectory);

        await sut.DeleteSaveDirectoryAsync(CancellationToken.None);

        fileSystem.Directory.Exists(SaveDirectory).ShouldBeFalse();
    }

    [Fact]
    public async Task when_deleting_save_directory_and_directory_does_not_exist_then_succeeds()
    {
        repo.GetBaseSaveDirectoryAsync(Arg.Any<CancellationToken>()).Returns(NonExistentDirectory);

        await Should.NotThrowAsync(() => sut.DeleteSaveDirectoryAsync(CancellationToken.None));
    }

    [Fact]
    public async Task when_deleting_save_directory_and_path_is_empty_then_no_directory_operations_occur()
    {
        repo.GetBaseSaveDirectoryAsync(Arg.Any<CancellationToken>()).Returns(string.Empty);

        await Should.NotThrowAsync(() => sut.DeleteSaveDirectoryAsync(CancellationToken.None));

        fileSystem.Statistics.Directory.Methods
            .ShouldNotContain(m => m.Name == "Delete");
    }

    [Fact]
    public async Task when_get_base_save_directory_throws_then_exception_propagates()
    {
        repo.GetBaseSaveDirectoryAsync(Arg.Any<CancellationToken>())
            .Returns<Task<string?>>(_ => Task.FromException<string?>(new InvalidOperationException("db error")));

        await Should.ThrowAsync<InvalidOperationException>(() => sut.DeleteSaveDirectoryAsync(CancellationToken.None));
    }
}
