using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.DisplaySettings;

public sealed class GivenAFileAuditFinishingOffTheUiThread
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<(FileAuditViewModel ViewModel, FakeFileAuditor Auditor)> ConfirmingAsync()
    {
        var photos = AttentionItems.Account("Photos");
        var source = new SwitchableSource(SyncOverviewFactory.Create([photos], [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);
        var auditor = new FakeFileAuditor { Delay = async () => await Task.Delay(20, CancellationToken).ConfigureAwait(false) };
        var viewModel = new FileAuditViewModel(overview, auditor, TestData.CreateLocalization());
        viewModel.Accounts[0].SelectCommand.Execute(null);
        viewModel.AuditCommand.Execute(null);

        return (viewModel, auditor);
    }

    [Fact]
    public async Task when_the_audit_finishes_on_a_pool_thread_then_property_changes_are_still_raised_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var (viewModel, _) = await ConfirmingAsync();
        var threads = new ConcurrentBag<int>();
        viewModel.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        await ui.RunAsync(() => viewModel.ConfirmCommand.ExecuteAsync(null)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        viewModel.IsShowingResult.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_audit_throws_on_a_pool_thread_then_the_failure_is_shown_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var (viewModel, auditor) = await ConfirmingAsync();
        auditor.Throws = new InvalidOperationException("boom");
        var threads = new ConcurrentBag<int>();
        viewModel.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        await ui.RunAsync(() => viewModel.ConfirmCommand.ExecuteAsync(null)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        viewModel.IsShowingError.ShouldBeTrue();
    }

    private sealed class SwitchableSource(SyncOverview initial) : ISyncOverviewSource
    {
        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(initial));
    }
}
