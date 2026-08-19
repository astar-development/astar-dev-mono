using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.Services;
using AStar.Dev.File.App.ViewModels;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;

namespace AStar.Dev.File.App.TestsUnit.ViewModels;

public class GivenADeletePendingViewModel
{
    private readonly IFileDeleteService _fileDeleteService = Substitute.For<IFileDeleteService>();
    private readonly IFileViewerService _fileViewerService = Substitute.For<IFileViewerService>();
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public GivenADeletePendingViewModel() => _dbContextFactory = CreateDbContextFactory(_databaseName);

    [Fact]
    public async Task when_constructed_with_pending_files_in_the_database_then_they_are_loaded()
    {
        await SeedScannedFileAsync("/data/docs/pending1.txt", pendingDelete: true);
        await SeedScannedFileAsync("/data/docs/pending2.txt", pendingDelete: true);
        await SeedScannedFileAsync("/data/docs/not-pending.txt", pendingDelete: false);

        var sut = CreateSut();
        await WaitForLoadAsync(sut);

        sut.PendingDeleteCount.ShouldBe(2);
        sut.PendingDeleteFiles.Count.ShouldBe(2);
    }

    [Fact]
    public void when_no_pending_files_exist_then_delete_all_command_cannot_execute()
    {
        var sut = CreateSut();

        sut.DeleteAllCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public async Task when_toggle_pending_delete_command_is_executed_with_null_then_does_not_throw()
    {
        var sut = CreateSut();

        await Should.NotThrowAsync(() => sut.TogglePendingDeleteCommand.Execute(null).ToTask(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_toggle_pending_delete_command_is_executed_then_the_item_is_removed_from_pending_files()
    {
        var scannedFile = await SeedScannedFileAsync("/data/docs/pending.txt", pendingDelete: true);
        var sut = CreateSut();
        await WaitForLoadAsync(sut);
        var item = sut.PendingDeleteFiles.Single();

        await sut.TogglePendingDeleteCommand.Execute(item).ToTask(TestContext.Current.CancellationToken);

        sut.PendingDeleteCount.ShouldBe(0);
        sut.PendingDeleteFiles.ShouldBeEmpty();

        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var saved = await db.ScannedFiles.SingleAsync(f => f.Id == scannedFile.Id, TestContext.Current.CancellationToken);
        saved.PendingDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task when_clear_markings_command_is_executed_with_no_pending_files_then_status_message_is_unchanged()
    {
        var sut = CreateSut();

        await sut.ClearMarkingsCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.StatusMessage.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_clear_markings_command_is_executed_then_all_markings_are_cleared()
    {
        await SeedScannedFileAsync("/data/docs/pending1.txt", pendingDelete: true);
        await SeedScannedFileAsync("/data/docs/pending2.txt", pendingDelete: true);
        var sut = CreateSut();
        await WaitForLoadAsync(sut);

        await sut.ClearMarkingsCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.PendingDeleteCount.ShouldBe(0);
        sut.StatusMessage.ShouldBe("All delete markings cleared.");

        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.ScannedFiles.AnyAsync(f => f.PendingDelete, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task when_delete_all_command_is_executed_then_the_files_are_deleted_and_removed_from_the_database()
    {
        var scannedFile = await SeedScannedFileAsync("/data/docs/pending.txt", pendingDelete: true);
        var sut = CreateSut();
        await WaitForLoadAsync(sut);

        await sut.DeleteAllCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        await _fileDeleteService.Received(1).DeleteFilesAsync(
            Arg.Is<IEnumerable<string>>(paths => paths.Single() == scannedFile.FullPath), moveToRecycleBin: true);
        sut.StatusMessage.ShouldStartWith("Successfully deleted");
        sut.PendingDeleteFiles.ShouldBeEmpty();

        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        (await db.ScannedFiles.AnyAsync(f => f.Id == scannedFile.Id, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task when_delete_all_command_fails_then_a_status_message_describes_the_error()
    {
        await SeedScannedFileAsync("/data/docs/pending.txt", pendingDelete: true);
        _fileDeleteService.DeleteFilesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns<Task>(_ => throw new InvalidOperationException("disk error"));
        var sut = CreateSut();
        await WaitForLoadAsync(sut);

        await sut.DeleteAllCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.StatusMessage.ShouldStartWith("Error deleting files:");
        sut.IsDeleting.ShouldBeFalse();
    }

    [Fact]
    public async Task when_view_file_command_is_executed_then_the_file_viewer_service_is_invoked()
    {
        var scannedFile = await SeedScannedFileAsync("/data/docs/file.txt");
        var displayItem = new ScannedFileDisplayItem(scannedFile);
        var sut = CreateSut();

        await sut.ViewFileCommand.Execute(displayItem).ToTask(TestContext.Current.CancellationToken);

        await _fileViewerService.Received(1).ViewFileAsync(displayItem);
    }

    [Fact]
    public void when_the_file_viewer_service_raises_file_view_requested_then_view_file_requested_is_forwarded()
    {
        var scannedFile = new ScannedFile
        {
            RootPath = "/data",
            FolderPath = "/data/docs",
            FileName = "file.txt",
            FullPath = "/data/docs/file.txt",
            FileType = FileType.Document,
            LastModified = DateTime.UtcNow
        };
        var displayItem = new ScannedFileDisplayItem(scannedFile);
        var sut = CreateSut();
        ScannedFileDisplayItem? raisedItem = null;
        sut.ViewFileRequested += item => raisedItem = item;

        _fileViewerService.FileViewRequested += Raise.Event<Action<ScannedFileDisplayItem>>(displayItem);

        raisedItem.ShouldBe(displayItem);
    }

    private DeletePendingViewModel CreateSut() => new(_dbContextFactory, _fileDeleteService, _fileViewerService);

    private static async Task WaitForLoadAsync(DeletePendingViewModel sut) =>
        await sut.WhenAnyValue(x => x.PendingDeleteCount)
            .Timeout(TimeSpan.FromSeconds(2))
            .FirstAsync(count => count > 0)
            .ToTask(TestContext.Current.CancellationToken);

    private static IDbContextFactory<FileAppDbContext> CreateDbContextFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<FileAppDbContext>().UseInMemoryDatabase(databaseName).Options;
        var factory = Substitute.For<IDbContextFactory<FileAppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new FileAppDbContext(options)));

        return factory;
    }

    private async Task<ScannedFile> SeedScannedFileAsync(string fullPath, bool pendingDelete = false)
    {
        var scannedFile = new ScannedFile
        {
            RootPath = "/data",
            FolderPath = "/data/docs",
            FileName = Path.GetFileName(fullPath),
            FullPath = fullPath,
            FileType = FileType.Document,
            LastModified = DateTime.UtcNow,
            SizeInBytes = 1024,
            PendingDelete = pendingDelete
        };

        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.ScannedFiles.Add(scannedFile);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return scannedFile;
    }
}
