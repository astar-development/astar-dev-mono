using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.SyncPairs;

public class GivenASyncPairViewModel
{
    [Fact]
    public void when_created_then_the_folders_account_and_direction_are_shown()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateUploadOnly(), SyncStatusFactory.CreateUpToDate());

        pair.Name.ShouldBe("Documents");
        pair.LocalPath.ShouldBe("Documents");
        pair.RemotePath.ShouldBe("/Documents");
        pair.AccountName.ShouldBe("Work");
        pair.DirectionLabel.ShouldBe("Upload only");
        pair.DirectionGlyph.ShouldBe("→");
    }

    [Fact]
    public void when_syncing_then_the_progress_is_the_percentage_as_a_fraction()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateSyncing(25));

        pair.Progress.ShouldBe(0.25);
    }

    [Fact]
    public void when_not_syncing_then_the_progress_is_complete()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateDownloadOnly(), SyncStatusFactory.CreateUpToDate());

        pair.Progress.ShouldBe(1);
        pair.DirectionLabel.ShouldBe("Download only");
    }

    [Fact]
    public void when_paused_then_the_progress_is_empty()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreatePaused());

        pair.Progress.ShouldBe(0);
    }

    [Fact]
    public void when_created_then_the_file_count_and_last_sync_are_summarised()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate());

        pair.SummaryText.ShouldBe("1,118 files · 4 min ago");
    }

    [Fact]
    public void when_created_then_the_last_sync_and_account_id_are_exposed()
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, "Documents", RelativePaths.Of("Documents"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 1_118, Option.Some(TestData.Now.AddMinutes(-4)));

        var pair = new SyncPairViewModel(model, account, TestData.CreateLocalization(), TestData.CreateTime());

        pair.LastSyncText.ShouldBe("4 min ago");
        pair.AccountId.ShouldBe(account.Id);
    }

    [Theory]
    [InlineData("TwoWay", "UploadOnly")]
    [InlineData("UploadOnly", "DownloadOnly")]
    [InlineData("DownloadOnly", "TwoWay")]
    public async Task when_the_direction_is_changed_then_the_next_direction_is_saved_and_the_overview_reloaded(string current, string expected)
    {
        var writer = new FakeSelectionRuleWriter();
        var reloads = 0;
        var pair = CreateChangeable(ParseDirection(current), writer, _ => { reloads++; return Task.CompletedTask; });

        await pair.ChangeDirectionCommand.ExecuteAsync(null);

        writer.Applied.Count.ShouldBe(1);
        writer.Applied[0].Count.ShouldBe(1);
        var change = writer.Applied[0][0].ShouldBeOfType<SelectionChange.ChangeDirection>();
        change.Pair.ShouldBe(pair.Id);
        change.Direction.ShouldBe(ParseDirection(expected));
        reloads.ShouldBe(1);
        pair.HasError.ShouldBeFalse();
        pair.ErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_saving_the_direction_fails_then_the_error_is_shown_and_the_overview_is_not_reloaded()
    {
        var writer = new FakeSelectionRuleWriter { Outcome = Result.Failure<Unit, ErrorResponse>(new ErrorResponse("database is locked")) };
        var reloads = 0;
        var pair = CreateChangeable(SyncDirectionFactory.CreateTwoWay(), writer, _ => { reloads++; return Task.CompletedTask; });

        await pair.ChangeDirectionCommand.ExecuteAsync(null);

        pair.HasError.ShouldBeTrue();
        pair.ErrorText.ShouldBe("Could not change the direction. Try again.");
        reloads.ShouldBe(0);
    }

    [Fact]
    public void when_created_then_the_change_direction_text_is_localised()
    {
        var pair = CreatePair(SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate());

        pair.ChangeDirectionText.ShouldBe("Change direction");
    }

    private static SyncDirection ParseDirection(string name) => name switch
    {
        "UploadOnly" => SyncDirectionFactory.CreateUploadOnly(),
        "DownloadOnly" => SyncDirectionFactory.CreateDownloadOnly(),
        _ => SyncDirectionFactory.CreateTwoWay(),
    };

    private static SyncPairViewModel CreateChangeable(SyncDirection direction, FakeSelectionRuleWriter writer, Func<CancellationToken, Task> reload)
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, "Documents", RelativePaths.Of("Documents"), Option.None<RemoteItemId>(), direction, SyncStatusFactory.CreateUpToDate(), 1_118, Option.Some(TestData.Now.AddMinutes(-4)));

        return new SyncPairViewModel(model, account, writer, reload, TestData.CreateLocalization(), TestData.CreateTime());
    }

    private static SyncPairViewModel CreatePair(SyncDirection direction, SyncStatus status)
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, "Documents", RelativePaths.Of("Documents"), Option.None<RemoteItemId>(), direction, status, 1_118, Option.Some(TestData.Now.AddMinutes(-4)));
        var localization = TestData.CreateLocalization();

        return new SyncPairViewModel(model, account, localization, TestData.CreateTime());
    }
}
