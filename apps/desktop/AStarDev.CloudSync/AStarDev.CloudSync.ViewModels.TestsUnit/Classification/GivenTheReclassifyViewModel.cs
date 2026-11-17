using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheReclassifyViewModel
{
    private readonly GetAllCountingTermStore terms = new();
    private readonly FakeClassificationReclassifier reclassifier = new();
    private readonly FakeReclassifyFreshnessCheck freshness = new();
    private readonly InlineSynchronizationContext context = new();
    private readonly ImmediateUiDispatcher dispatcher = new();
    private readonly ReclassifyProgressSource progressSource = new();

    private ReclassifyViewModel Create()
    {
        var localization = TestData.CreateLocalization();
        var termList = new ClassificationTermListViewModel(terms, new FakeCategoryStore(), new FakeFileClassificationStore(), localization);

        return new ReclassifyViewModel(reclassifier, reclassifier, progressSource, dispatcher, freshness, termList, localization);
    }

    private static ReclassifyCounts Counts(int files, int batches) => ReclassifyCountsFactory.Create(files, batches, 0);

    [Fact]
    public void when_created_then_it_is_idle_with_the_button_text_and_no_status()
    {
        var viewModel = Create();

        (viewModel.State, viewModel.IsRunning, viewModel.HasStatus, viewModel.ButtonText, viewModel.CancelText, viewModel.StatusText, viewModel.ReclassifyCommand.CanExecute(null), viewModel.CancelCommand.CanExecute(null))
            .ShouldBe((ReclassifyStateFactory.CreateIdle(), false, false, "Reclassify", "Cancel", string.Empty, true, false));
    }

    [Fact]
    public async Task when_a_run_completes_then_the_counts_are_shown_and_the_term_list_reloads()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateCompleted(Counts(1234, 3));
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        (viewModel.State, viewModel.IsRunning, viewModel.HasStatus, viewModel.StatusText, terms.GetAllCount).ShouldBe((ReclassifyStateFactory.CreateCompleted(Counts(1234, 3)), false, true, "Reclassified 1,234 files in 3 batches", 1));
    }

    [Fact]
    public async Task when_the_run_reports_progress_then_it_is_marshalled_through_the_context_current_when_the_command_started()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Reports = [ReclassifyProgressFactory.Create(500, 1), ReclassifyProgressFactory.Create(1000, 2)];
        reclassifier.Gate = new TaskCompletionSource();
        var viewModel = Create();
        var running = viewModel.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;

        var during = (viewModel.State, viewModel.IsRunning, viewModel.StatusText, viewModel.ReclassifyCommand.CanExecute(null), viewModel.CancelCommand.CanExecute(null), context.Posts);
        reclassifier.Gate.SetResult();
        await running;

        during.ShouldBe((ReclassifyStateFactory.CreateRunning(1000), true, "Reclassifying: 1,000 files done", false, true, 2));
    }

    [Fact]
    public async Task when_the_run_reports_skipped_files_then_the_running_status_shows_the_skipped_count()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Reports = [ReclassifyProgressFactory.Create(0, 0, 500), ReclassifyProgressFactory.Create(1000, 1, 1500)];
        reclassifier.Gate = new TaskCompletionSource();
        var viewModel = Create();
        var running = viewModel.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;

        var during = (viewModel.State, viewModel.StatusText);
        reclassifier.Gate.SetResult();
        await running;

        during.ShouldBe((ReclassifyStateFactory.CreateRunning(1000, 1500), "Reclassifying: 1,000 files done, 1,500 skipped"));
    }

    [Fact]
    public async Task when_a_run_completes_with_skipped_files_then_the_skipped_count_is_shown()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateCompleted(ReclassifyCountsFactory.Create(1234, 3, 0, 2500));
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        viewModel.StatusText.ShouldBe("Reclassified 1,234 files in 3 batches, 2,500 skipped because their account was busy or removed");
    }

    [Fact]
    public async Task when_a_run_skips_every_file_then_the_completed_status_shows_nothing_written_and_the_skipped_count()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateCompleted(ReclassifyCountsFactory.Create(0, 0, 0, 3));
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        viewModel.StatusText.ShouldBe("Reclassified 0 files in 0 batches, 3 skipped because their account was busy or removed");
    }

    [Fact]
    public void when_a_running_state_is_created_with_a_negative_skipped_count_then_it_becomes_zero()
    {
        var state = ReclassifyStateFactory.CreateRunning(5, -1);

        state.ShouldBe(ReclassifyStateFactory.CreateRunning(5, 0));
    }

    [Fact]
    public async Task when_disposed_while_running_then_the_run_is_cancelled_and_the_term_list_reloads()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Gate = new TaskCompletionSource();
        var viewModel = Create();
        var running = viewModel.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;

        viewModel.Dispose();
        await running;

        (viewModel.State, terms.GetAllCount).ShouldBe((ReclassifyStateFactory.CreateCancelled(Counts(0, 0)), 1));
    }

    [Fact]
    public void when_disposed_while_idle_then_nothing_happens()
    {
        var viewModel = Create();

        viewModel.Dispose();

        viewModel.State.ShouldBe(ReclassifyStateFactory.CreateIdle());
    }

    [Fact]
    public async Task when_cancelled_then_the_cancelled_state_is_shown_and_the_term_list_reloads()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Gate = new TaskCompletionSource();
        var viewModel = Create();
        var running = viewModel.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;

        viewModel.CancelCommand.Execute(null);
        await running;

        (viewModel.State, viewModel.StatusText, viewModel.CancelCommand.CanExecute(null), terms.GetAllCount).ShouldBe((ReclassifyStateFactory.CreateCancelled(Counts(0, 0)), "Reclassify cancelled after 0 files", false, 1));
    }

    [Fact]
    public async Task when_the_run_fails_then_the_failed_state_is_shown_and_the_term_list_reloads()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateFailed(Counts(40, 1));
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        (viewModel.State, viewModel.StatusText, terms.GetAllCount).ShouldBe((ReclassifyStateFactory.CreateFailed(Counts(40, 1)), "Reclassify failed after 40 files", 1));
    }

    [Fact]
    public async Task when_another_run_is_in_progress_then_the_already_running_state_is_shown_and_the_term_list_is_not_reloaded()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateAlreadyRunning();
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        (viewModel.State, viewModel.StatusText, viewModel.IsRunning, terms.GetAllCount).ShouldBe((ReclassifyStateFactory.CreateAlreadyRunning(), "A reclassify is already running", false, 0));
    }

    [Fact]
    public async Task when_run_again_after_an_outcome_then_a_new_run_starts()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);
        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        reclassifier.StartCount.ShouldBe(2);
    }

    [Fact]
    public void when_created_then_no_rerun_is_recommended_before_the_check_runs()
    {
        var viewModel = Create();

        (viewModel.IsRerunRecommended, freshness.CheckCount).ShouldBe((false, 0));
    }

    [Fact]
    public async Task when_the_check_finds_the_links_stale_then_a_rerun_is_recommended_with_the_localised_text()
    {
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateStale());
        var viewModel = Create();

        await viewModel.CheckCommand.ExecuteAsync(null);

        (viewModel.IsRerunRecommended, viewModel.RerunRecommendedText).ShouldBe((true, "The classification rules have changed since the last full reclassify, or none has finished yet. Reclassify to bring the file tags up to date."));
    }

    [Fact]
    public async Task when_the_check_finds_the_links_up_to_date_then_no_rerun_is_recommended()
    {
        var viewModel = Create();

        await viewModel.CheckCommand.ExecuteAsync(null);

        (viewModel.IsRerunRecommended, freshness.CheckCount).ShouldBe((false, 1));
    }

    [Fact]
    public async Task when_the_check_fails_then_no_rerun_is_recommended()
    {
        freshness.Outcome = Result.Failure<ReclassifyFreshness, ClassificationFailure>(ClassificationFailureFactory.CreateUnavailable());
        var viewModel = Create();

        await viewModel.CheckCommand.ExecuteAsync(null);

        viewModel.IsRerunRecommended.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_check_result_changes_then_the_recommendation_is_announced()
    {
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateStale());
        var viewModel = Create();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await viewModel.CheckCommand.ExecuteAsync(null);

        changed.ShouldContain(nameof(ReclassifyViewModel.IsRerunRecommended));
    }

    [Fact]
    public async Task when_a_run_completes_then_the_check_runs_again_and_clears_the_recommendation()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateStale());
        var viewModel = Create();
        await viewModel.CheckCommand.ExecuteAsync(null);
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateUpToDate());

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        (viewModel.IsRerunRecommended, freshness.CheckCount).ShouldBe((false, 2));
    }

    [Fact]
    public async Task when_another_run_is_in_progress_then_the_check_does_not_run_again()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Outcome = ReclassifyOutcomeFactory.CreateAlreadyRunning();
        var viewModel = Create();

        await viewModel.ReclassifyCommand.ExecuteAsync(null);

        freshness.CheckCount.ShouldBe(0);
    }

    [Fact]
    public void when_a_run_is_recorded_elsewhere_then_the_recommendation_is_checked_again_on_the_ui_dispatcher()
    {
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateStale());
        using var viewModel = Create();

        reclassifier.RaiseRunRecorded();

        (viewModel.IsRerunRecommended, dispatcher.Invocations, freshness.CheckCount).ShouldBe((true, 1, 1));
    }

    [Fact]
    public void when_created_then_it_listens_for_recorded_runs_and_when_disposed_then_it_stops()
    {
        var viewModel = Create();
        var whileAlive = reclassifier.RunRecordedSubscribers;

        viewModel.Dispose();

        (whileAlive, reclassifier.RunRecordedSubscribers).ShouldBe((1, 0));
    }

    [Fact]
    public void when_disposed_then_a_run_recorded_afterwards_is_ignored()
    {
        var viewModel = Create();
        viewModel.Dispose();

        reclassifier.RaiseRunRecorded();

        (dispatcher.Invocations, freshness.CheckCount).ShouldBe((0, 0));
    }

    private IReclassifyRunScope StartForeignRun() => progressSource.Begin(new Progress<ReclassifyProgress>());

    [Fact]
    public void when_the_page_opens_mid_run_then_the_current_progress_is_shown_at_once_without_cancel()
    {
        using var foreign = StartForeignRun();
        foreign.Report(ReclassifyProgressFactory.Create(1500, 3));

        var viewModel = Create();

        (viewModel.State, viewModel.IsRunning, viewModel.IsBusy, viewModel.HasStatus, viewModel.StatusText, viewModel.ReclassifyCommand.CanExecute(null), viewModel.CancelCommand.CanExecute(null))
            .ShouldBe((ReclassifyStateFactory.CreateObserving(1500, 0), false, true, true, "Reclassifying: 1,500 files done", false, false));
    }

    [Fact]
    public void when_a_foreign_run_reports_progress_then_the_status_follows_it()
    {
        var viewModel = Create();
        using var foreign = StartForeignRun();

        foreign.Report(ReclassifyProgressFactory.Create(500, 1));
        foreign.Report(ReclassifyProgressFactory.Create(1000, 2, 40));

        (viewModel.State, viewModel.StatusText, viewModel.ReclassifyCommand.CanExecute(null)).ShouldBe((ReclassifyStateFactory.CreateObserving(1000, 40), "Reclassifying: 1,000 files done, 40 skipped", false));
    }

    [Fact]
    public void when_a_foreign_run_finishes_then_the_page_is_idle_the_button_is_enabled_the_terms_reload_and_the_freshness_is_rechecked()
    {
        var viewModel = Create();
        var foreign = StartForeignRun();
        foreign.Report(ReclassifyProgressFactory.Create(500, 1));

        foreign.Dispose();

        (viewModel.State, viewModel.IsBusy, viewModel.ReclassifyCommand.CanExecute(null), terms.GetAllCount, freshness.CheckCount).ShouldBe((ReclassifyStateFactory.CreateIdle(), false, true, 1, 1));
    }

    [Fact]
    public void when_a_foreign_run_finishes_and_the_rerun_is_no_longer_needed_then_the_recommendation_clears()
    {
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateStale());
        using var viewModel = Create();
        _ = viewModel.CheckCommand.ExecuteAsync(null);
        var foreign = StartForeignRun();
        foreign.Report(ReclassifyProgressFactory.Create(500, 1));
        freshness.Outcome = Result.Success<ReclassifyFreshness, ClassificationFailure>(ReclassifyFreshnessFactory.CreateUpToDate());

        foreign.Dispose();

        viewModel.IsRerunRecommended.ShouldBeFalse();
    }

    [Fact]
    public void when_a_foreign_run_that_never_reported_ends_then_nothing_changes()
    {
        var viewModel = Create();
        var foreign = StartForeignRun();

        foreign.Dispose();

        (viewModel.State, terms.GetAllCount, freshness.CheckCount).ShouldBe((ReclassifyStateFactory.CreateIdle(), 0, 0));
    }

    [Fact]
    public async Task when_the_page_started_the_run_then_foreign_reports_do_not_replace_its_state_and_cancel_stays_available()
    {
        using var installed = InlineSynchronizationContext.Install(context);
        reclassifier.Gate = new TaskCompletionSource();
        var viewModel = Create();
        var running = viewModel.ReclassifyCommand.ExecuteAsync(null);
        await reclassifier.Started.Task;
        using var foreign = StartForeignRun();

        foreign.Report(ReclassifyProgressFactory.Create(9999, 9));
        var during = (viewModel.State, viewModel.IsRunning, viewModel.CancelCommand.CanExecute(null));
        reclassifier.Gate.SetResult();
        await running;

        during.ShouldBe((ReclassifyStateFactory.CreateRunning(0), true, true));
    }

    [Fact]
    public void when_created_then_it_listens_for_progress_and_when_disposed_then_it_stops()
    {
        var viewModel = Create();
        viewModel.Dispose();
        using var foreign = StartForeignRun();

        foreign.Report(ReclassifyProgressFactory.Create(500, 1));

        (viewModel.State, dispatcher.Invocations).ShouldBe((ReclassifyStateFactory.CreateIdle(), 0));
    }

    [Fact]
    public void when_a_negative_count_is_observed_then_it_becomes_zero()
    {
        ReclassifyStateFactory.CreateObserving(-1, -2).ShouldBe(ReclassifyStateFactory.CreateObserving(0, 0));
    }

    [Fact]
    public void when_the_view_model_is_disposed_with_a_queued_progress_change_then_running_it_does_nothing()
    {
        var queued = new QueuedUiDispatcher();
        var localization = TestData.CreateLocalization();
        var termList = new ClassificationTermListViewModel(terms, new FakeCategoryStore(), new FakeFileClassificationStore(), localization);
        var viewModel = new ReclassifyViewModel(reclassifier, reclassifier, progressSource, queued, freshness, termList, localization);
        var foreign = StartForeignRun();
        foreign.Report(ReclassifyProgressFactory.Create(10, 1));
        queued.RunQueued();
        foreign.Dispose();
        viewModel.Dispose();

        queued.RunQueued();

        (viewModel.State, terms.GetAllCount, freshness.CheckCount).ShouldBe((ReclassifyStateFactory.CreateObserving(10, 0), 0, 0));
    }
}
