using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
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
    private readonly IUiDispatcher dispatcher = new InlineUiDispatcher();

    private static SyncedItemSearchResult MakeResult(string localPath) =>
        new(1, TestAccountId, TestItemId, "/remote/image.png", localPath, DateTimeOffset.UtcNow, 1024, []);

    [Fact]
    public async Task when_file_does_not_exist_then_thumbnail_stays_null()
    {
        fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Image);
        var vm = new SyncedFileResultViewModel(MakeResult("/no/such/file/astar_test.png"), fileTypeClassifier, fileOpenerService, dispatcher);

        await vm.LoadThumbnailAsync();

        vm.Thumbnail.ShouldBeNull();
    }

    [Fact]
    public async Task when_file_type_is_not_image_then_thumbnail_stays_null()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllBytesAsync(tmpPath, PngFixtures.OneByOnePng, TestContext.Current.CancellationToken);
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Document);
            var vm = new SyncedFileResultViewModel(MakeResult(tmpPath), fileTypeClassifier, fileOpenerService, dispatcher);

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
        var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        await File.WriteAllBytesAsync(tmpPath, PngFixtures.OneByOnePng, TestContext.Current.CancellationToken);
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Image);
            var vm = new SyncedFileResultViewModel(MakeResult(tmpPath), fileTypeClassifier, fileOpenerService, dispatcher);

            await vm.LoadThumbnailAsync();

            vm.Thumbnail.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }
}
