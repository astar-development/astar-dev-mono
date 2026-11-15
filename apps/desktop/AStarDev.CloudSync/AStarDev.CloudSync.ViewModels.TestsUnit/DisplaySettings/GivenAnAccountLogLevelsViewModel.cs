using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.DisplaySettings;

public sealed class GivenAnAccountLogLevelsViewModel
{
    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    private static readonly CloudAccount Work = AttentionItems.Account("Work");

    private readonly FakeAccountLogLevelSettings settings = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<(SyncOverviewViewModel Overview, SwitchableSource Source)> OverviewOfAsync(params CloudAccount[] accounts)
    {
        var source = new SwitchableSource(SyncOverviewFactory.Create(accounts, [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);

        return (overview, source);
    }

    private async Task<AccountLogLevelsViewModel> LoadedAsync(params CloudAccount[] accounts)
    {
        var (overview, _) = await OverviewOfAsync(accounts);
        var viewModel = new AccountLogLevelsViewModel(overview, settings, TestData.CreateLocalization());
        await viewModel.LoadAsync(CancellationToken);

        return viewModel;
    }

    [Fact]
    public async Task when_there_are_no_accounts_then_there_are_no_rows_and_the_empty_text_is_shown()
    {
        var viewModel = await LoadedAsync();

        (viewModel.Rows.Count, viewModel.HasAccounts, viewModel.NoAccountsText).ShouldBe((0, false, "Add an account to choose its logging."));
    }

    [Fact]
    public async Task when_there_are_accounts_then_each_has_a_row_with_its_saved_level_and_its_name_in_the_label()
    {
        settings.Seed(Work.Id, AccountLogLevelFactory.CreateVerbose());

        var viewModel = await LoadedAsync(Photos, Work);

        viewModel.Rows.Select(row => (row.LabelText, row.IsVerbose)).ShouldBe([("Verbose logging for Photos", false), ("Verbose logging for Work", true)]);
    }

    [Fact]
    public async Task when_the_texts_are_read_then_the_heading_and_help_are_localised()
    {
        var viewModel = await LoadedAsync(Photos);

        (viewModel.HeadingText, viewModel.HelpText).ShouldBe(("Diagnostic logging", "Verbose logging writes detail to this computer's log files for one account. It is never sent anywhere."));
    }

    [Fact]
    public async Task when_a_row_is_toggled_on_then_the_account_is_saved_as_verbose()
    {
        var viewModel = await LoadedAsync(Photos);

        await viewModel.Rows[0].ToggleCommand.ExecuteAsync(null);

        viewModel.Rows[0].IsVerbose.ShouldBeTrue();
        settings.Saved.Select(saved => saved.AccountId).ShouldBe([Photos.Id]);
        settings.Saved[0].Level.ShouldBeOfType<AccountLogLevel.Verbose>();
    }

    [Fact]
    public async Task when_a_verbose_row_is_toggled_then_the_account_is_saved_as_standard()
    {
        settings.Seed(Photos.Id, AccountLogLevelFactory.CreateVerbose());
        var viewModel = await LoadedAsync(Photos);

        await viewModel.Rows[0].ToggleCommand.ExecuteAsync(null);

        (viewModel.Rows[0].IsVerbose, settings.Saved.Single().Level).ShouldBe((false, AccountLogLevelFactory.CreateStandard()));
    }

    [Fact]
    public async Task when_one_row_is_toggled_then_the_other_accounts_are_untouched()
    {
        var viewModel = await LoadedAsync(Photos, Work);

        await viewModel.Rows[1].ToggleCommand.ExecuteAsync(null);

        viewModel.Rows[0].IsVerbose.ShouldBeFalse();
        settings.Saved.Select(saved => saved.AccountId).ShouldBe([Work.Id]);
    }

    [Fact]
    public async Task when_saving_fails_then_the_row_keeps_its_level_and_shows_a_message_that_clears_on_the_next_success()
    {
        var viewModel = await LoadedAsync(Photos);
        settings.WriteFails = true;

        await viewModel.Rows[0].ToggleCommand.ExecuteAsync(null);
        var failed = (viewModel.Rows[0].IsVerbose, viewModel.Rows[0].ErrorText);
        settings.WriteFails = false;
        await viewModel.Rows[0].ToggleCommand.ExecuteAsync(null);

        (failed, viewModel.Rows[0].IsVerbose, viewModel.Rows[0].ErrorText).ShouldBe(((false, "The logging setting could not be saved. Try again."), true, string.Empty));
    }

    [Fact]
    public async Task when_the_saved_levels_cannot_be_read_then_a_message_is_shown_and_the_accounts_count_as_standard()
    {
        settings.ReadFails = true;

        var viewModel = await LoadedAsync(Photos);

        (viewModel.ErrorText, viewModel.Rows.Single().IsVerbose).ShouldBe(("The logging settings could not be read.", false));
    }

    [Fact]
    public async Task when_the_accounts_change_then_the_rows_follow_with_their_saved_levels()
    {
        var (overview, source) = await OverviewOfAsync(Photos);
        var viewModel = new AccountLogLevelsViewModel(overview, settings, TestData.CreateLocalization());
        await viewModel.LoadAsync(CancellationToken);
        settings.Seed(Work.Id, AccountLogLevelFactory.CreateVerbose());
        source.Overview = SyncOverviewFactory.Create([Photos, Work], [], [], [], [], 0, Option.None<DateTimeOffset>());

        await overview.LoadAsync(CancellationToken);
        await Task.Delay(100, CancellationToken);

        viewModel.Rows.Select(row => (row.LabelText, row.IsVerbose)).ShouldBe([("Verbose logging for Photos", false), ("Verbose logging for Work", true)]);
    }

    private sealed class SwitchableSource(SyncOverview initial) : ISyncOverviewSource
    {
        public SyncOverview Overview { get; set; } = initial;

        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(Overview));
    }
}
