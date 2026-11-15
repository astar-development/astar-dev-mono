using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.SyncPairs;

public sealed class GivenASyncPairChangedOffTheUiThread
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_save_succeeds_on_a_pool_thread_then_the_reload_runs_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var reloadThreads = new ConcurrentBag<int>();
        var pair = Create(Result.Success<Unit, ErrorResponse>(Unit.Instance), _ =>
        {
            reloadThreads.Add(Environment.CurrentManagedThreadId);

            return Task.CompletedTask;
        });

        await ui.RunAsync(() => pair.ChangeDirectionCommand.ExecuteAsync(null)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        reloadThreads.Distinct().ShouldBe([ui.ThreadId]);
    }

    [Fact]
    public async Task when_the_save_fails_on_a_pool_thread_then_the_error_is_raised_on_the_ui_thread()
    {
        using var ui = new SingleThreadContext();
        var pair = Create(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")), _ => Task.CompletedTask);
        var threads = new ConcurrentBag<int>();
        pair.PropertyChanged += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        await ui.RunAsync(() => pair.ChangeDirectionCommand.ExecuteAsync(null)).WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);

        threads.Distinct().ShouldBe([ui.ThreadId]);
        pair.HasError.ShouldBeTrue();
    }

    private static SyncPairViewModel Create(Result<Unit, ErrorResponse> outcome, Func<CancellationToken, Task> reload)
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, "Documents", RelativePaths.Of("Documents"), Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay(), SyncPairStateFactory.Create(SyncStatusFactory.CreateUpToDate(), 1_118, Option.Some(TestData.Now.AddMinutes(-4))));

        return new SyncPairViewModel(model, account, new SlowWriter(outcome), reload, TestData.CreateLocalization(), TestData.CreateTime());
    }

    private sealed class SlowWriter(Result<Unit, ErrorResponse> outcome) : ILeasedSelectionRuleWriter
    {
        public async Task<Result<Unit, ErrorResponse>> ApplyAsync(AccountId accountId, IReadOnlyList<SelectionChange> changes, CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

            return outcome;
        }
    }
}
