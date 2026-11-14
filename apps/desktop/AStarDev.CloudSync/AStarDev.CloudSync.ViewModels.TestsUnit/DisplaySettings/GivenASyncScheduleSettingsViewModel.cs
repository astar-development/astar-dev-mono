using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.DisplaySettings;

public class GivenASyncScheduleSettingsViewModel
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static SyncScheduleSettingsViewModel Create(FakeSyncScheduleSettings settings) => new(settings, TestData.CreateLocalization());

    private static string SelectedLabel(SyncScheduleSettingsViewModel viewModel) => viewModel.Options.Single(option => option.IsSelected).Label;

    [Fact]
    public void when_created_then_the_choices_are_the_five_intervals_then_manual_only_with_localised_labels()
    {
        var viewModel = Create(new FakeSyncScheduleSettings());

        viewModel.Options.Select(option => option.Label).ShouldBe(["Every 15 minutes", "Every 30 minutes", "Every hour", "Every 2 hours", "Every 4 hours", "Manual only"]);
        viewModel.HeadingText.ShouldBe("Automatic sync");
    }

    [Fact]
    public void when_created_then_the_default_hour_is_selected_and_there_is_no_error()
    {
        var viewModel = Create(new FakeSyncScheduleSettings());

        SelectedLabel(viewModel).ShouldBe("Every hour");
        viewModel.ErrorText.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(15, "Every 15 minutes")]
    [InlineData(30, "Every 30 minutes")]
    [InlineData(120, "Every 2 hours")]
    [InlineData(240, "Every 4 hours")]
    public async Task when_an_interval_was_saved_then_loading_selects_it(int minutes, string label)
    {
        var settings = new FakeSyncScheduleSettings { Current = Result.Success<SyncSchedule, ErrorResponse>(SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(minutes))) };
        var viewModel = Create(settings);

        await viewModel.LoadAsync(CancellationToken);

        SelectedLabel(viewModel).ShouldBe(label);
    }

    [Fact]
    public async Task when_manual_only_was_saved_then_loading_selects_it()
    {
        var settings = new FakeSyncScheduleSettings { Current = Result.Success<SyncSchedule, ErrorResponse>(SyncScheduleFactory.CreateManualOnly()) };
        var viewModel = Create(settings);

        await viewModel.LoadAsync(CancellationToken);

        SelectedLabel(viewModel).ShouldBe("Manual only");
    }

    [Fact]
    public async Task when_the_schedule_cannot_be_read_then_a_localised_message_is_shown_without_the_error_text_and_the_default_stays()
    {
        var settings = new FakeSyncScheduleSettings { Current = Result.Failure<SyncSchedule, ErrorResponse>(new ErrorResponse("C:/secret/db failed")) };
        var viewModel = Create(settings);

        await viewModel.LoadAsync(CancellationToken);

        viewModel.ErrorText.ShouldBe("The automatic sync setting could not be read.");
        SelectedLabel(viewModel).ShouldBe("Every hour");
    }

    [Fact]
    public async Task when_an_option_is_chosen_then_it_is_saved_and_selected()
    {
        var settings = new FakeSyncScheduleSettings();
        var viewModel = Create(settings);

        await viewModel.SelectAsync(SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(30)), CancellationToken);

        settings.Saved.ShouldBe([SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(30))]);
        SelectedLabel(viewModel).ShouldBe("Every 30 minutes");
    }

    [Fact]
    public async Task when_the_selected_option_is_chosen_again_then_nothing_is_saved()
    {
        var settings = new FakeSyncScheduleSettings();
        var viewModel = Create(settings);

        await viewModel.SelectAsync(SyncScheduleFactory.CreateDefault(), CancellationToken);

        settings.Saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_manual_only_is_chosen_then_it_is_saved_and_selected()
    {
        var settings = new FakeSyncScheduleSettings();
        var viewModel = Create(settings);

        await viewModel.SelectAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);

        settings.Saved.ShouldBe([SyncScheduleFactory.CreateManualOnly()]);
        SelectedLabel(viewModel).ShouldBe("Manual only");
    }

    [Fact]
    public async Task when_the_save_fails_then_the_previous_choice_stays_and_a_localised_message_is_shown_without_the_error_text()
    {
        var settings = new FakeSyncScheduleSettings { SaveFails = true };
        var viewModel = Create(settings);

        await viewModel.SelectAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);

        SelectedLabel(viewModel).ShouldBe("Every hour");
        viewModel.ErrorText.ShouldBe("The automatic sync setting could not be saved.");
    }

    [Fact]
    public async Task when_a_save_works_after_a_failed_one_then_the_message_is_cleared()
    {
        var settings = new FakeSyncScheduleSettings { SaveFails = true };
        var viewModel = Create(settings);
        await viewModel.SelectAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);
        settings.SaveFails = false;

        await viewModel.SelectAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);

        viewModel.ErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_option_command_runs_then_the_schedule_is_saved()
    {
        var settings = new FakeSyncScheduleSettings();
        var viewModel = Create(settings);

        viewModel.Options[0].SelectCommand.Execute(null);
        while (settings.Saved.Count == 0)
            await Task.Delay(5, CancellationToken);

        settings.Saved.ShouldBe([SyncScheduleFactory.CreateEvery(TimeSpan.FromMinutes(15))]);
    }

    [Fact]
    public async Task when_the_selection_changes_then_the_message_and_selection_changes_are_raised()
    {
        var viewModel = Create(new FakeSyncScheduleSettings { SaveFails = true });
        var raised = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raised.Add(args.PropertyName);

        await viewModel.SelectAsync(SyncScheduleFactory.CreateManualOnly(), CancellationToken);

        raised.ShouldContain(nameof(SyncScheduleSettingsViewModel.ErrorText));
    }
}
