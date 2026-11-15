using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Conflicts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Overview.TestsUnit;

public class GivenAnOverviewWithThrottledAccounts
{
    private static readonly AccountId Throttled = AccountIdFactory.Create(Guid.Parse("0b1c2d3e-4f50-4617-8293-a4b5c6d7e8f9"));
    private static readonly AccountId Calm = AccountIdFactory.Create(Guid.Parse("1f2e3d4c-5b6a-4798-8a9b-0c1d2e3f4a5b"));

    [Fact]
    public async Task when_an_account_is_throttled_often_then_only_its_card_suggests_a_lower_concurrency()
    {
        var hints = new FakeAccountThrottleHints();
        hints.Set(Throttled, ThrottleHintFactory.CreateSuggestLowerConcurrency());
        using var provider = CreateProvider(hints);

        var overview = await provider.GetRequiredService<ISyncOverviewSource>().GetOverviewAsync(TestContext.Current.CancellationToken).MatchAsync(read => read, error => throw new InvalidOperationException(error.Message));

        overview.Accounts.Select(account => (account.Id, account.ThrottleHint.GetType())).ShouldBe([(Throttled, typeof(ThrottleHint.SuggestLowerConcurrency)), (Calm, typeof(ThrottleHint.Quiet))]);
    }

    private static ServiceProvider CreateProvider(FakeAccountThrottleHints hints) => new ServiceCollection()
            .AddSingleton<ICloudSyncDatabase>(new FakeCloudSyncDatabase())
            .AddSingleton<IAccountStore>(new FakeAccountStore { All = () => Result.Success<IReadOnlyList<StoredAccount>, ErrorResponse>([StoredAccount(Throttled), StoredAccount(Calm)]) })
            .AddSingleton<IAccountSignInStates>(new FakeAccountSignInStates())
            .AddSingleton<ISyncPairReader>(new FakeSyncPairReader())
            .AddSingleton<ISyncRunStatusSource>(new FakeSyncRunStatusSource())
            .AddSingleton<IAccountPauseReader>(new FakeAccountPauseReader())
            .AddSingleton<ISyncBaselineCounter>(new FakeSyncBaselineCounter())
            .AddSingleton<IActivityLogReader>(new FakeActivityLogReader())
            .AddSingleton<ISyncRunJournal>(new InMemorySyncRunJournal())
            .AddSingleton<IAccountThrottleHints>(hints)
            .AddLogging()
            .AddSingleton<ISyncAttentionReader>(new FakeAttentionReader())
            .AddSyncOverview()
            .BuildServiceProvider();

    private static StoredAccount StoredAccount(AccountId id) =>
        StoredAccountFactory.Create(id, AccountRegistrationFactory.Create(ProviderAccountKeyFactory.Create("00000000-0000-0000-4a1b-2c3d4e5f6a7b.9188040d-6c67-4c5b-b112-36a304b66dad").Match(key => key, error => throw new InvalidOperationException(error.Message)), CloudProviderFactory.CreateOneDrive(), "Captain Flint", "captain.flint@example.com", DriveIdFactory.Create("8f2a1b3c4d5e6f70").Match(driveId => driveId, error => throw new InvalidOperationException(error.Message)), StorageUsageFactory.Create(1_500, 5_000)));
}
