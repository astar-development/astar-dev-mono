using System.Globalization;
using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.Localization;
using AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Activity;

public sealed class GivenAnActivityViewModelWithManyConflicts
{
    [Fact]
    public void when_the_conflict_count_exceeds_999_then_the_badge_text_has_a_thousands_delimiter()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");
        var loc = Substitute.For<ILocalizationService>();
        var sut = new ActivityViewModel(Substitute.For<ISyncRepository>(), Substitute.For<ISyncEventAggregator>(), new ConflictItemViewModelFactory(Substitute.For<ISyncService>(), loc), new ActivityItemViewModelFactory(loc), new InlineUiDispatcher(), loc);

        sut.ConflictCount = 12345;

        CultureInfo.CurrentCulture = originalCulture;
        sut.ConflictBadgeText.ShouldBe("12,345");
    }
}
