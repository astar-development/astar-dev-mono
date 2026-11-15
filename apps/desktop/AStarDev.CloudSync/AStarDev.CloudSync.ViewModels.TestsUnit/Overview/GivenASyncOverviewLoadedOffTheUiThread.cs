using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public sealed class GivenASyncOverviewLoadedOffTheUiThread
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_read_finishes_on_a_pool_thread_then_property_changes_are_still_raised_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var viewModel = new SyncOverviewViewModel(new SlowSource(Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([AttentionItems.Account("Photos")], [], [], [], [], 0, Option.None<DateTimeOffset>()))), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var threads = new ConcurrentBag<int>();
        viewModel.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        await ui.RunAsync(() => viewModel.LoadAsync(CancellationToken)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
    }

    [Fact]
    public async Task when_the_source_reports_a_failure_on_a_pool_thread_then_the_failed_state_is_raised_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var viewModel = new SyncOverviewViewModel(new SlowSource(Result.Failure<SyncOverview, ErrorResponse>(new ErrorResponse("locked"))), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        var threads = new ConcurrentBag<int>();
        viewModel.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        await ui.RunAsync(() => viewModel.LoadAsync(CancellationToken)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        viewModel.LoadErrorText.ShouldBe("Could not load your accounts and sync pairs. The details have been logged.");
    }

    private sealed class SlowSource(Result<SyncOverview, ErrorResponse> answer) : ISyncOverviewSource
    {
        public async Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

            return answer;
        }
    }
}
