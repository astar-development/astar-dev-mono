using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Conflicts;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Conflicts;

internal sealed class ConflictListFixture
{
    public static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    public static readonly CloudAccount Work = AttentionItems.Account("Work");

    public ConflictListFixture() : this(Microsoft.Extensions.Logging.Abstractions.NullLogger<ConflictListViewModel>.Instance)
    {
    }

    public ConflictListFixture(ILogger<ConflictListViewModel> logger) =>
        List = new ConflictListViewModel(new ConflictListSources(new FixedAccountsSource(Photos, Work), Reader, Pairs), Resolver, Decider, new ConflictResolutionPlanner(), TestData.CreateLocalization(), logger);

    public FakeAttentionReader Reader { get; } = new();

    public FakeConflictResolver Resolver { get; } = new();

    public FakeHeldDeletionDecider Decider { get; } = new();

    public FakeSyncPairReader Pairs { get; } = new();

    public ConflictListViewModel List { get; }
}
