using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.DisplaySettings;

public class GivenATrashRetentionSettingsViewModel
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static TrashRetentionSettingsViewModel Create(FakeTrashRetentionSettings settings) => new(settings, TestData.CreateLocalization());

    private static string SelectedLabel(TrashRetentionSettingsViewModel viewModel) => viewModel.Options.Single(option => option.IsSelected).Label;

    [Fact]
    public void when_created_then_the_choices_are_the_five_retentions_with_localised_labels_and_thirty_days_is_selected()
    {
        var viewModel = Create(new FakeTrashRetentionSettings());

        viewModel.Options.Select(option => option.Label).ShouldBe(["7 days", "14 days", "30 days", "90 days", "1 year"]);
        viewModel.HeadingText.ShouldBe("Keep removed files for");
        SelectedLabel(viewModel).ShouldBe("30 days");
        viewModel.ErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_retention_was_saved_then_loading_selects_it()
    {
        var settings = new FakeTrashRetentionSettings { Current = Result.Success<LocalTrashOptions, ErrorResponse>(LocalTrashOptionsFactory.CreateFromDays(90)) };
        var viewModel = Create(settings);

        await viewModel.LoadAsync(CancellationToken);

        SelectedLabel(viewModel).ShouldBe("90 days");
    }

    [Fact]
    public async Task when_the_retention_cannot_be_read_then_a_localised_message_is_shown_without_the_error_text_and_the_default_stays()
    {
        var settings = new FakeTrashRetentionSettings { Current = Result.Failure<LocalTrashOptions, ErrorResponse>(new ErrorResponse("C:/secret/db failed")) };
        var viewModel = Create(settings);

        await viewModel.LoadAsync(CancellationToken);

        viewModel.ErrorText.ShouldBe("The trash retention setting could not be read.");
        SelectedLabel(viewModel).ShouldBe("30 days");
    }

    [Fact]
    public async Task when_an_option_is_chosen_then_it_is_saved_and_selected()
    {
        var settings = new FakeTrashRetentionSettings();
        var viewModel = Create(settings);

        await viewModel.SelectAsync(LocalTrashOptionsFactory.CreateFromDays(7), CancellationToken);

        settings.Saved.ShouldBe([LocalTrashOptionsFactory.CreateFromDays(7)]);
        SelectedLabel(viewModel).ShouldBe("7 days");
    }

    [Fact]
    public async Task when_the_selected_option_is_chosen_again_then_nothing_is_saved()
    {
        var settings = new FakeTrashRetentionSettings();
        var viewModel = Create(settings);

        await viewModel.SelectAsync(LocalTrashOptionsFactory.Create(), CancellationToken);

        settings.Saved.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_save_fails_then_the_previous_choice_stays_and_a_localised_message_is_shown_without_the_error_text()
    {
        var settings = new FakeTrashRetentionSettings { SaveFails = true };
        var viewModel = Create(settings);

        await viewModel.SelectAsync(LocalTrashOptionsFactory.CreateFromDays(7), CancellationToken);

        SelectedLabel(viewModel).ShouldBe("30 days");
        viewModel.ErrorText.ShouldBe("The trash retention setting could not be saved.");
    }

    [Fact]
    public async Task when_a_save_works_after_a_failed_one_then_the_message_is_cleared()
    {
        var settings = new FakeTrashRetentionSettings { SaveFails = true };
        var viewModel = Create(settings);
        await viewModel.SelectAsync(LocalTrashOptionsFactory.CreateFromDays(7), CancellationToken);
        settings.SaveFails = false;

        await viewModel.SelectAsync(LocalTrashOptionsFactory.CreateFromDays(7), CancellationToken);

        viewModel.ErrorText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_option_command_runs_then_the_retention_is_saved()
    {
        var settings = new FakeTrashRetentionSettings();
        var viewModel = Create(settings);

        viewModel.Options[0].SelectCommand.Execute(null);
        while (settings.Saved.Count == 0)
            await Task.Delay(1, CancellationToken);

        settings.Saved.ShouldBe([LocalTrashOptionsFactory.CreateFromDays(7)]);
    }
}
