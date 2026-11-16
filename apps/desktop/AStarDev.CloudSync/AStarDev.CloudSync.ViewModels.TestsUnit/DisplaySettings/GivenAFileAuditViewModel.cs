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

public sealed class GivenAFileAuditViewModel
{
    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    private static readonly CloudAccount Work = AttentionItems.Account("Work");

    private readonly FakeFileAuditor auditor = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<(SyncOverviewViewModel Overview, SwitchableSource Source)> OverviewOfAsync(params CloudAccount[] accounts)
    {
        var source = new SwitchableSource(SyncOverviewFactory.Create(accounts, [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);

        return (overview, source);
    }

    private async Task<FileAuditViewModel> CreatedAsync(params CloudAccount[] accounts)
    {
        var (overview, _) = await OverviewOfAsync(accounts);

        return new FileAuditViewModel(overview, auditor, TestData.CreateLocalization());
    }

    private async Task<FileAuditViewModel> ConfirmingAsync()
    {
        var viewModel = await CreatedAsync(Photos, Work);
        viewModel.Accounts[1].SelectCommand.Execute(null);
        viewModel.AuditCommand.Execute(null);

        return viewModel;
    }

    [Fact]
    public async Task when_there_are_no_accounts_then_there_are_no_rows_and_the_empty_text_is_shown()
    {
        var viewModel = await CreatedAsync();

        (viewModel.Accounts.Count, viewModel.HasAccounts, viewModel.NoAccountsText).ShouldBe((0, false, "Add an account to audit it."));
    }

    [Fact]
    public async Task when_there_are_accounts_then_each_has_a_row_and_none_is_chosen_so_the_audit_cannot_start()
    {
        var viewModel = await CreatedAsync(Photos, Work);

        viewModel.Accounts.Select(row => (row.Name, row.IsSelected)).ShouldBe([("Photos", false), ("Work", false)]);
        (viewModel.HasAccounts, viewModel.CanAudit, viewModel.AuditCommand.CanExecute(null)).ShouldBe((true, false, false));
    }

    [Fact]
    public async Task when_an_account_is_chosen_then_only_it_is_selected_and_the_audit_can_start()
    {
        var viewModel = await CreatedAsync(Photos, Work);

        viewModel.Accounts[1].SelectCommand.Execute(null);

        viewModel.Accounts.Select(row => row.IsSelected).ShouldBe([false, true]);
        (viewModel.CanAudit, viewModel.AuditCommand.CanExecute(null)).ShouldBe((true, true));
    }

    [Fact]
    public async Task when_the_texts_are_read_then_they_are_localised()
    {
        var viewModel = await CreatedAsync(Photos);

        (viewModel.HeadingText, viewModel.AuditText, viewModel.ConfirmText, viewModel.CancelText).ShouldBe(("File audit", "Audit", "Run audit", "Cancel"));
        viewModel.HelpText.ShouldBe("Counts the files in an account's local sync folder and in its cloud account. Folders and files that are not synced are left out.");
    }

    [Fact]
    public async Task when_audit_is_pressed_then_the_slow_warning_shows_and_nothing_runs_yet()
    {
        var viewModel = await ConfirmingAsync();

        (viewModel.IsConfirming, viewModel.ShowsAuditButton, viewModel.IsRunning).ShouldBe((true, false, false));
        viewModel.WarningText.ShouldBe("This reads every file in the cloud account and can take a long time, especially for a large account. You can keep using CloudSync while it runs.");
        auditor.Audited.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_warning_is_declined_then_the_audit_does_not_run_and_the_button_returns()
    {
        var viewModel = await ConfirmingAsync();

        viewModel.CancelCommand.Execute(null);

        (viewModel.IsConfirming, viewModel.ShowsAuditButton, viewModel.Step).ShouldBe((false, true, FileAuditStepFactory.CreateIdle()));
        auditor.Audited.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_warning_is_accepted_then_the_chosen_account_is_audited_and_the_counts_show()
    {
        var viewModel = await ConfirmingAsync();

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        auditor.Audited.ShouldBe([Work.Id]);
        (viewModel.IsShowingResult, viewModel.IsRunning, viewModel.ShowsAuditButton).ShouldBe((true, false, true));
        (viewModel.LocalFilesText, viewModel.CloudFilesText).ShouldBe(("Local files: 1,234", "Cloud files: 5,678"));
    }

    [Fact]
    public async Task when_the_audit_is_running_then_it_says_which_account_and_offers_cancel_only()
    {
        auditor.Gate = new TaskCompletionSource();
        var viewModel = await ConfirmingAsync();

        var running = viewModel.ConfirmCommand.ExecuteAsync(null);

        (viewModel.IsRunning, viewModel.ShowsAuditButton, viewModel.IsConfirming, viewModel.RunningText).ShouldBe((true, false, false, "Auditing Work…"));
        auditor.Gate.SetResult();
        await running;
    }

    [Fact]
    public async Task when_another_account_is_chosen_while_running_then_the_choice_is_ignored()
    {
        auditor.Gate = new TaskCompletionSource();
        var viewModel = await ConfirmingAsync();
        var running = viewModel.ConfirmCommand.ExecuteAsync(null);

        viewModel.Accounts[0].SelectCommand.Execute(null);

        viewModel.Accounts.Select(row => row.IsSelected).ShouldBe([false, true]);
        auditor.Gate.SetResult();
        await running;
    }

    [Fact]
    public async Task when_a_running_audit_is_cancelled_then_it_stops_with_no_result_and_the_button_returns()
    {
        auditor.Gate = new TaskCompletionSource();
        var viewModel = await ConfirmingAsync();
        var running = viewModel.ConfirmCommand.ExecuteAsync(null);

        viewModel.CancelCommand.Execute(null);
        await running;

        (viewModel.IsRunning, viewModel.IsShowingResult, viewModel.IsShowingError, viewModel.ShowsAuditButton).ShouldBe((false, false, false, true));
    }

    [Theory]
    [InlineData("root", "The local sync folder could not be read.")]
    [InlineData("rules", "The folder selection could not be read.")]
    [InlineData("cloud", "The cloud account could not be read. Try again later.")]
    public async Task when_the_audit_fails_then_the_matching_message_shows_and_no_counts_do(string kind, string message)
    {
        auditor.Outcome = Result.Failure<FileAuditCounts, FileAuditFailure>(kind switch
        {
            "root" => FileAuditFailureFactory.CreateRootUnavailable(),
            "rules" => FileAuditFailureFactory.CreateRulesUnavailable(),
            _ => FileAuditFailureFactory.CreateCloudUnavailable(ProviderErrorFactory.CreateUnexpected()),
        });
        var viewModel = await ConfirmingAsync();

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        (viewModel.IsShowingError, viewModel.IsShowingResult, viewModel.ErrorText, viewModel.LocalFilesText).ShouldBe((true, false, message, string.Empty));
    }

    [Fact]
    public async Task when_the_auditor_throws_unexpectedly_then_the_failure_shows_instead_of_crashing()
    {
        auditor.Throws = new InvalidOperationException("boom");
        var viewModel = await ConfirmingAsync();

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        (viewModel.IsShowingError, viewModel.ErrorText).ShouldBe((true, "The cloud account could not be read. Try again later."));
    }

    [Fact]
    public async Task when_a_different_account_is_chosen_after_a_result_then_the_result_clears()
    {
        var viewModel = await ConfirmingAsync();
        await viewModel.ConfirmCommand.ExecuteAsync(null);

        viewModel.Accounts[0].SelectCommand.Execute(null);

        (viewModel.IsShowingResult, viewModel.LocalFilesText, viewModel.Step).ShouldBe((false, string.Empty, FileAuditStepFactory.CreateIdle()));
    }

    [Fact]
    public async Task when_audit_is_pressed_with_no_account_chosen_then_nothing_happens()
    {
        var viewModel = await CreatedAsync(Photos);

        viewModel.AuditCommand.Execute(null);

        (viewModel.IsConfirming, viewModel.Step).ShouldBe((false, FileAuditStepFactory.CreateIdle()));
    }

    [Fact]
    public async Task when_confirm_is_run_without_the_warning_then_nothing_is_audited()
    {
        var viewModel = await CreatedAsync(Photos);
        viewModel.Accounts[0].SelectCommand.Execute(null);

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        (auditor.Audited.Count, viewModel.Step).ShouldBe((0, FileAuditStepFactory.CreateIdle()));
    }

    [Fact]
    public async Task when_the_accounts_change_then_the_rows_follow_and_the_choice_is_kept()
    {
        var (overview, source) = await OverviewOfAsync(Photos);
        var viewModel = new FileAuditViewModel(overview, auditor, TestData.CreateLocalization());
        viewModel.Accounts[0].SelectCommand.Execute(null);
        source.Overview = SyncOverviewFactory.Create([Photos, Work], [], [], [], [], 0, Option.None<DateTimeOffset>());

        await overview.LoadAsync(CancellationToken);

        viewModel.Accounts.Select(row => (row.Name, row.IsSelected)).ShouldBe([("Photos", true), ("Work", false)]);
    }

    [Fact]
    public async Task when_the_chosen_account_is_removed_then_the_choice_clears_and_the_audit_cannot_start()
    {
        var (overview, source) = await OverviewOfAsync(Photos, Work);
        var viewModel = new FileAuditViewModel(overview, auditor, TestData.CreateLocalization());
        viewModel.Accounts[1].SelectCommand.Execute(null);
        source.Overview = SyncOverviewFactory.Create([Photos], [], [], [], [], 0, Option.None<DateTimeOffset>());

        await overview.LoadAsync(CancellationToken);

        viewModel.Accounts.Select(row => row.IsSelected).ShouldBe([false]);
        viewModel.CanAudit.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_audit_runs_twice_then_each_run_has_its_own_indicator()
    {
        var viewModel = await ConfirmingAsync();
        await viewModel.ConfirmCommand.ExecuteAsync(null);
        var first = viewModel.RunIndicators.Single();
        viewModel.AuditCommand.Execute(null);

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        viewModel.RunIndicators.Single().ShouldNotBeSameAs(first);
    }

    private sealed class SwitchableSource(SyncOverview initial) : ISyncOverviewSource
    {
        public SyncOverview Overview { get; set; } = initial;

        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(Overview));
    }
}
