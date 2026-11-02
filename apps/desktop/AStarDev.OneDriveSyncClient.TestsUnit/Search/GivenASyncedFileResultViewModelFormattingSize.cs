using System.Globalization;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.Search;
using AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Search;

[Collection("AvaloniaHeadless")]
public sealed class GivenASyncedFileResultViewModelFormattingSize
{
    private static string FormattedSizeFor(long? sizeInBytes)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");
        var result = new SyncedItemSearchResult(1, new AccountId("acc-1"), new OneDriveItemId("item-1"), "/remote/file.jpg", "/tmp/nonexistent_file_astar.jpg", DateTimeOffset.UtcNow, sizeInBytes, []);
        var sut = new SyncedFileResultViewModel(result, Substitute.For<IFileTypeClassifier>(), Substitute.For<IFileOpenerService>(), new InlineUiDispatcher(), Substitute.For<ILocalizationService>(), _ => Task.CompletedTask);
        CultureInfo.CurrentCulture = originalCulture;

        return sut.FormattedSize;
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(512L, "512 B")]
    [InlineData(1023L, "1,023 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(5L * 1024 * 1024, "5.0 MB")]
    [InlineData(2L * 1024 * 1024 * 1024, "2.0 GB")]
    [InlineData(1536L * 1024 * 1024 * 1024, "1,536.0 GB")]
    public void when_the_size_is_formatted_then_it_is_human_readable_with_thousands_delimiters(long? sizeInBytes, string expected) => FormattedSizeFor(sizeInBytes).ShouldBe(expected);
}
