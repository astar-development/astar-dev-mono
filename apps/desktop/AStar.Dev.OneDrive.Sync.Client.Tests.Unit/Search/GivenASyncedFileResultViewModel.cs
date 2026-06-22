using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Search;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;
using Avalonia.Headless.XUnit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Search;

public sealed class GivenASyncedFileResultViewModel
{
    private static readonly AccountId TestAccountId = new("acc-1");
    private static readonly OneDriveItemId TestItemId = new("item-1");

    private readonly IFileOpenerService fileOpenerService = Substitute.For<IFileOpenerService>();
    private readonly IFileTypeClassifier fileTypeClassifier = Substitute.For<IFileTypeClassifier>();
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();
    private readonly IUiDispatcher dispatcher = new InlineUiDispatcher();

    private static SyncedItemSearchResult MakeResult(string localPath) =>
        new(1, TestAccountId, TestItemId, "/remote/image.png", localPath, DateTimeOffset.UtcNow, 1024, []);

    private SyncedFileResultViewModel CreateSut(string localPath, Func<CancellationToken, Task>? onDelete = null) =>
        new(MakeResult(localPath), fileTypeClassifier, fileOpenerService, dispatcher, loc, onDelete ?? ((_) => Task.CompletedTask));

    [Fact]
    public async Task when_file_does_not_exist_then_thumbnail_stays_null()
    {
        fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Image);
        var vm = CreateSut("/no/such/file/astar_test.png");

        await vm.LoadThumbnailAsync();

        vm.Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task when_file_type_is_not_image_then_thumbnail_stays_null()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllBytesAsync(tmpPath, PngFixtures.OneByOnePng, TestContext.Current.CancellationToken);
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Document);
            var vm = CreateSut(tmpPath);

            await vm.LoadThumbnailAsync();

            vm.Thumbnail.ShouldBeNull();
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [AvaloniaFact]
    public async Task when_file_exists_and_type_is_image_then_thumbnail_is_set()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        await File.WriteAllBytesAsync(tmpPath, PngFixtures.OneByOnePng, TestContext.Current.CancellationToken);
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Image);
            var vm = CreateSut(tmpPath);

            await vm.LoadThumbnailAsync();

            vm.Thumbnail.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [Fact]
    public async Task when_delete_command_is_executed_then_on_delete_callback_is_invoked()
    {
        bool callbackInvoked = false;
        var vm = CreateSut("/no/such/file/astar_test.png", _ => { callbackInvoked = true; return Task.CompletedTask; });

        await vm.DeleteFileCommand.ExecuteAsync(null);

        callbackInvoked.ShouldBeTrue();
    }

    [Fact]
    public void when_delete_button_text_is_read_then_it_delegates_to_localisation_service()
    {
        loc.GetLocal("Search.Result.Delete.Button").Returns("Delete");
        var vm = CreateSut("/no/such/file/astar_test.png");

        vm.DeleteButtonText.ShouldBe("Delete");
    }

    [Fact]
    public async Task when_cancel_thumbnail_load_is_called_during_load_then_thumbnail_stays_null()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        await File.WriteAllBytesAsync(tmpPath, PngFixtures.OneByOnePng, TestContext.Current.CancellationToken);
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Image);
            var vm = CreateSut(tmpPath);

            _ = vm.LoadThumbnailAsync();
            vm.CancelThumbnailLoad();
            await Task.Yield();

            vm.Thumbnail.ShouldBeNull();
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }
}
