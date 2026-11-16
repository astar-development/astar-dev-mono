using AStarDev.CloudSync.Classification;
using AStarDev.CloudSync.TestHelpers.Classification;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Classification;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Classification;

public class GivenTheReclassifyViewModel
{
    private readonly GetAllCountingTermStore terms = new();
    private readonly FakeClassificationReclassifier reclassifier = new();
    private readonly InlineSynchronizationContext context = new();

    private ReclassifyViewModel Create()
    {
        var localization = TestData.CreateLocalization();
        var termList = new ClassificationTermListViewModel(terms, new FakeCategoryStore(), new FakeFileClassificationStore(), localization);

        return new ReclassifyViewModel(reclassifier, termList, localization);
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
}
