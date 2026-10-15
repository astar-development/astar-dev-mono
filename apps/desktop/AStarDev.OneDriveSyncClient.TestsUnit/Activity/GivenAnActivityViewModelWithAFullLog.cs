using AStarDev.OneDriveSyncClient.Activity;
using AStarDev.OneDriveSyncClient.Conflicts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Pipeline;
using AStarDev.OneDriveSyncClient.Localization;
using AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Pipeline;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Activity;

public sealed class GivenAnActivityViewModelWithAFullLog
{
    private const string AccountId = "acc-1";
    private const int InfoItemsBeyondTheLimit = 150;

    private readonly ILocalizationService localizationService = Substitute.For<ILocalizationService>();
    private readonly ActivityItemViewModelFactory activityItemViewModelFactory;
    private readonly ActivityViewModel sut;

    public GivenAnActivityViewModelWithAFullLog()
    {
        activityItemViewModelFactory = new ActivityItemViewModelFactory(localizationService);
        sut = new ActivityViewModel(Substitute.For<ISyncRepository>(), Substitute.For<ISyncEventAggregator>(), new ConflictItemViewModelFactory(Substitute.For<AStarDev.OneDriveSyncClient.Infrastructure.Sync.ISyncService>(), localizationService), activityItemViewModelFactory, new InlineUiDispatcher(), localizationService);
    }

    [Fact]
    public void when_info_items_exceed_the_log_size_then_an_earlier_error_is_retained()
    {
        sut.AddActivityItem(activityItemViewModelFactory.CreateError(AccountId, "user@example.com", "upload failed"));

        AddInfoItems(InfoItemsBeyondTheLimit);

        sut.LogItems.ShouldContain(item => item.Type == ActivityItemType.Error);
    }

    [Fact]
    public void when_info_items_exceed_the_log_size_then_the_oldest_info_items_are_removed()
    {
        AddInfoItems(InfoItemsBeyondTheLimit);

        sut.LogItems.Count.ShouldBe(100);
    }

    [Fact]
    public void when_errors_are_present_then_they_do_not_count_towards_the_log_size()
    {
        sut.AddActivityItem(activityItemViewModelFactory.CreateError(AccountId, "user@example.com", "upload failed"));
        sut.AddActivityItem(activityItemViewModelFactory.CreateError(AccountId, "user@example.com", "download failed"));

        AddInfoItems(InfoItemsBeyondTheLimit);

        sut.LogItems.Count(item => item.Type == ActivityItemType.Info).ShouldBe(100);
    }

    private void AddInfoItems(int count)
    {
        for (int index = 0; index < count; index++)
            sut.AddActivityItem(activityItemViewModelFactory.CreateInfo(AccountId, $"file-{index}"));
    }
}
