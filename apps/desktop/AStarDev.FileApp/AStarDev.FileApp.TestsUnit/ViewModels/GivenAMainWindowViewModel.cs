using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AStar.Dev.File.App.Data;
using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.Services;
using AStar.Dev.File.App.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.File.App.TestsUnit.ViewModels;

public class GivenAMainWindowViewModel
{
    private readonly IFileScannerService _fileScannerService = Substitute.For<IFileScannerService>();
    private readonly IFolderPickerService _folderPickerService = Substitute.For<IFolderPickerService>();
    private readonly IFileViewerService _fileViewerService = Substitute.For<IFileViewerService>();
    private readonly IDbContextFactory<FileAppDbContext> _dbContextFactory;
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public GivenAMainWindowViewModel() => _dbContextFactory = CreateDbContextFactory(_databaseName);

    [Fact]
    public void when_constructed_then_page_sizes_contains_the_expected_options()
        => MainWindowViewModel.PageSizes.ShouldBe([25, 50, 75, 100, 125, 150, 175, 200]);

    [Fact]
    public void when_constructed_then_default_page_size_is_fifty()
        => CreateSut().PageSize.ShouldBe(50);

    [Fact]
    public void when_total_file_count_is_zero_then_total_pages_is_one()
    {
        var sut = CreateSut();

        sut.TotalFileCount = 0;

        sut.TotalPages.ShouldBe(1);
    }

    [Theory]
    [InlineData(1, 50, 1)]
    [InlineData(50, 50, 1)]
    [InlineData(51, 50, 2)]
    [InlineData(101, 50, 3)]
    public void when_total_file_count_changes_then_total_pages_is_computed(int totalFileCount, int pageSize, int expected)
    {
        var sut = CreateSut();

        sut.PageSize = pageSize;
        sut.TotalFileCount = totalFileCount;

        sut.TotalPages.ShouldBe(expected);
    }

    [Fact]
    public async Task when_paging_info_is_read_then_it_is_formatted_with_page_total_and_count()
    {
        for (int i = 0; i < 120; i++)
            await SeedScannedFileAsync($"/data/docs/file{i}.txt");

        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";
        await sut.LoadFromDatabaseCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        await sut.NextPageCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.PagingInfo.ShouldBe("PAGE 2 OF 3  [120 FILES]");
    }

    [Fact]
    public void when_scanning_then_select_folder_command_cannot_execute()
    {
        var sut = CreateSut();

        sut.IsScanning = true;

        sut.SelectFolderCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void when_selected_folder_path_is_empty_then_start_scan_command_cannot_execute()
    {
        var sut = CreateSut();

        sut.SelectedFolderPath = string.Empty;

        sut.StartScanCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void when_selected_folder_path_is_set_and_not_scanning_then_start_scan_command_can_execute()
    {
        var sut = CreateSut();

        sut.SelectedFolderPath = "/data";
        sut.IsScanning = false;

        sut.StartScanCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public async Task when_select_folder_command_is_executed_with_a_chosen_path_then_selected_folder_path_is_updated()
    {
        _folderPickerService.OpenFolderPickerAsync().Returns(Task.FromResult<string?>("/chosen/folder"));
        var sut = CreateSut();

        await sut.SelectFolderCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.SelectedFolderPath.ShouldBe("/chosen/folder");
    }

    [Fact]
    public async Task when_select_folder_command_is_executed_with_a_chosen_path_then_the_path_is_persisted_to_the_database()
    {
        _folderPickerService.OpenFolderPickerAsync().Returns(Task.FromResult<string?>("/chosen/folder"));
        var sut = CreateSut();

        await sut.SelectFolderCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var setting = await db.AppSettings.SingleAsync(TestContext.Current.CancellationToken);
        setting.Key.ShouldBe("SelectedFolderPath");
        setting.Value.ShouldBe("/chosen/folder");
    }

    [Fact]
    public async Task when_select_folder_command_is_executed_with_no_chosen_path_then_selected_folder_path_is_unchanged()
    {
        _folderPickerService.OpenFolderPickerAsync().Returns(Task.FromResult<string?>(null));
        var sut = CreateSut();
        string before = sut.SelectedFolderPath;

        await sut.SelectFolderCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.SelectedFolderPath.ShouldBe(before);
    }

    [Fact]
    public async Task when_start_scan_command_is_executed_then_the_scanner_service_is_invoked()
    {
        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";

        await sut.StartScanCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        await _fileScannerService.Received(1).ScanAsync("/data", Arg.Any<IProgress<ScanProgressUpdate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_start_scan_command_completes_then_is_scanning_is_false()
    {
        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";

        await sut.StartScanCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.IsScanning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_start_scan_command_is_cancelled_then_a_cancellation_status_message_is_added()
    {
        _fileScannerService.ScanAsync(Arg.Any<string>(), Arg.Any<IProgress<ScanProgressUpdate>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException());
        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";

        await sut.StartScanCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.StatusMessages.ShouldContain(message => message.Contains("CANCELLED"));
        sut.IsScanning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_toggle_pending_delete_command_is_executed_with_null_then_does_not_throw()
    {
        var sut = CreateSut();

        await Should.NotThrowAsync(() => sut.TogglePendingDeleteCommand.Execute(null).ToTask(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_toggle_pending_delete_command_is_executed_then_the_flag_is_flipped_in_the_database()
    {
        var scannedFile = await SeedScannedFileAsync("/data/docs/file.txt", pendingDelete: false);
        var displayItem = new ScannedFileDisplayItem(scannedFile);
        var sut = CreateSut();

        await sut.TogglePendingDeleteCommand.Execute(displayItem).ToTask(TestContext.Current.CancellationToken);

        displayItem.PendingDelete.ShouldBeTrue();
        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var saved = await db.ScannedFiles.SingleAsync(TestContext.Current.CancellationToken);
        saved.PendingDelete.ShouldBeTrue();
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
    public async Task when_the_file_viewer_service_raises_file_view_requested_then_view_file_requested_is_forwarded()
    {
        var scannedFile = await SeedScannedFileAsync("/data/docs/file.txt");
        var displayItem = new ScannedFileDisplayItem(scannedFile);
        var sut = CreateSut();
        ScannedFileDisplayItem? raisedItem = null;
        sut.ViewFileRequested += item => raisedItem = item;

        _fileViewerService.FileViewRequested += Raise.Event<Action<ScannedFileDisplayItem>>(displayItem);

        raisedItem.ShouldBe(displayItem);
    }

    [Fact]
    public async Task when_open_delete_window_command_is_executed_then_open_delete_window_requested_is_raised()
    {
        var sut = CreateSut();
        bool raised = false;
        sut.OpenDeleteWindowRequested += () => raised = true;

        await sut.OpenDeleteWindowCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        raised.ShouldBeTrue();
    }

    [Fact]
    public async Task when_toggle_duplicates_only_command_is_executed_then_show_duplicates_only_is_flipped()
    {
        var sut = CreateSut();
        sut.ShowDuplicatesOnly.ShouldBeFalse();

        await sut.ToggleDuplicatesOnlyCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.ShowDuplicatesOnly.ShouldBeTrue();
    }

    [Fact]
    public void when_on_the_first_page_then_previous_page_command_cannot_execute()
    {
        var sut = CreateSut();
        sut.CurrentPage = 1;

        sut.PreviousPageCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void when_not_on_the_last_page_then_next_page_command_can_execute()
    {
        var sut = CreateSut();
        sut.PageSize = 50;
        sut.TotalFileCount = 200;
        sut.CurrentPage = 1;

        sut.NextPageCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public async Task when_next_page_command_is_executed_then_current_page_is_incremented()
    {
        var sut = await CreateSutWithPagedFilesAsync(fileCount: 200, pageSize: 50);

        await sut.NextPageCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.CurrentPage.ShouldBe(2);
    }

    [Fact]
    public async Task when_last_page_command_is_executed_then_current_page_becomes_the_total_pages()
    {
        var sut = await CreateSutWithPagedFilesAsync(fileCount: 200, pageSize: 50);

        await sut.LastPageCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.CurrentPage.ShouldBe(4);
    }

    [Fact]
    public async Task when_first_page_command_is_executed_then_current_page_becomes_one()
    {
        var sut = await CreateSutWithPagedFilesAsync(fileCount: 200, pageSize: 50);
        await sut.LastPageCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        await sut.FirstPageCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task when_showing_duplicates_only_then_files_that_share_a_size_with_another_file_are_returned()
    {
        await SeedScannedFileAsync("/data/docs/a.txt", sizeInBytes: 100);
        await SeedScannedFileAsync("/data/docs/b.txt", sizeInBytes: 100);
        await SeedScannedFileAsync("/data/docs/unique.txt", sizeInBytes: 999);
        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";
        sut.ShowDuplicatesOnly = true;

        await sut.LoadFromDatabaseCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.ScannedFiles.Count.ShouldBe(2);
        sut.ScannedFiles.ShouldAllBe(file => file.SizeInBytes == 100);
    }

    [Fact]
    public async Task when_not_showing_duplicates_only_then_all_files_under_the_selected_folder_are_returned()
    {
        await SeedScannedFileAsync("/data/docs/a.txt", sizeInBytes: 100);
        await SeedScannedFileAsync("/data/docs/b.txt", sizeInBytes: 100);
        await SeedScannedFileAsync("/data/docs/unique.txt", sizeInBytes: 999);
        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";
        sut.ShowDuplicatesOnly = false;

        await sut.LoadFromDatabaseCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        sut.ScannedFiles.Count.ShouldBe(3);
    }

    private MainWindowViewModel CreateSut() => new(_fileScannerService, _folderPickerService, _fileViewerService, _dbContextFactory);

    private async Task<MainWindowViewModel> CreateSutWithPagedFilesAsync(int fileCount, int pageSize)
    {
        for (int i = 0; i < fileCount; i++)
            await SeedScannedFileAsync($"/data/docs/file{i}.txt");

        var sut = CreateSut();
        sut.SelectedFolderPath = "/data";
        sut.PageSize = pageSize;
        await sut.LoadFromDatabaseCommand.Execute().ToTask(TestContext.Current.CancellationToken);

        return sut;
    }

    private static IDbContextFactory<FileAppDbContext> CreateDbContextFactory(string databaseName)
    {
        var options = new DbContextOptionsBuilder<FileAppDbContext>().UseInMemoryDatabase(databaseName).Options;
        var factory = Substitute.For<IDbContextFactory<FileAppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new FileAppDbContext(options)));

        return factory;
    }

    private async Task<ScannedFile> SeedScannedFileAsync(string fullPath, bool pendingDelete = false, long sizeInBytes = 1024)
    {
        var scannedFile = new ScannedFile
        {
            RootPath = "/data",
            FolderPath = "/data/docs",
            FileName = Path.GetFileName(fullPath),
            FullPath = fullPath,
            FileType = FileType.Document,
            LastModified = DateTime.UtcNow,
            SizeInBytes = sizeInBytes,
            PendingDelete = pendingDelete
        };

        await using var db = await _dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.ScannedFiles.Add(scannedFile);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return scannedFile;
    }
}
