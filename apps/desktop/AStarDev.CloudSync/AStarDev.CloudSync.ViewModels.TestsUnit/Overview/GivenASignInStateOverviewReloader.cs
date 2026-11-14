using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Overview;

public class GivenASignInStateOverviewReloader
{
    [Fact]
    public void when_a_sign_in_state_changes_then_the_overview_reloads_on_the_ui_thread()
    {
        var source = new CountingOverviewSource();
        var states = new FakeAccountSignInStates();
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        _ = new SignInStateOverviewReloader(states, overview, new ImmediateUiDispatcher());

        states.Record(AccountIdFactory.Create(Guid.NewGuid()), AccountSignInStateFactory.CreateSignInRequired());
        states.Forget(AccountIdFactory.Create(Guid.NewGuid()));

        source.Reads.ShouldBe(2);
    }

    private sealed class CountingOverviewSource : ISyncOverviewSource
    {
        public int Reads { get; private set; }

        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken)
        {
            Reads++;

            return Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([], [], [], [], [], 0, Option.None<DateTimeOffset>())));
        }
    }
}
