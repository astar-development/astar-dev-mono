using System.ComponentModel;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenADatabaseHealthBannerViewModel
{
    private readonly FakeDatabaseHealthGate gate = new();

    private DatabaseHealthBannerViewModel CreateBanner() => new(gate, new ImmediateUiDispatcher(), TestData.CreateLocalization());

    [Fact]
    public void when_the_check_has_not_finished_then_there_is_no_text()
    {
        CreateBanner().Text.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_database_is_healthy_then_there_is_no_text()
    {
        var banner = CreateBanner();

        gate.Finish(DatabaseHealthStatusFactory.CreateHealthy());

        banner.Text.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_database_is_damaged_then_the_text_says_syncing_is_stopped_and_the_files_are_untouched()
    {
        var banner = CreateBanner();

        gate.Finish(DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateDamaged()));

        banner.Text.ShouldBe("Syncing is stopped because the CloudSync database failed its integrity check. Your files have not been changed. Restore the database from a backup, then restart the app: the interrupted sync resumes by itself.");
    }

    [Fact]
    public void when_the_database_cannot_be_read_then_the_text_says_so()
    {
        var banner = CreateBanner();

        gate.Finish(DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateUnreadable()));

        banner.Text.ShouldBe("Syncing is stopped because the CloudSync database could not be read. Your files have not been changed. Check that the file is not locked or damaged, then restart the app: the interrupted sync resumes by itself.");
    }

    [Fact]
    public void when_the_check_finishes_then_the_text_change_is_announced()
    {
        var banner = CreateBanner();
        var changed = new List<string?>();
        ((INotifyPropertyChanged)banner).PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        gate.Finish(DatabaseHealthStatusFactory.CreateFailed(DatabaseHealthFailureFactory.CreateDamaged()));

        changed.ShouldContain(nameof(DatabaseHealthBannerViewModel.Text));
    }
}
