using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountThatIsOftenThrottled
{
    [Fact]
    public void when_the_provider_keeps_throttling_the_account_then_a_hint_to_lower_the_transfers_is_shown()
    {
        var account = CreateViewModel(ThrottleHintFactory.CreateSuggestLowerConcurrency());

        (account.HasThrottleHint, account.ThrottleHintText).ShouldBe((true, "Your cloud provider keeps slowing this account down. Try fewer transfers at once in the account settings."));
    }

    [Fact]
    public void when_the_account_is_not_throttled_often_then_no_hint_is_shown()
    {
        var account = CreateViewModel(ThrottleHintFactory.CreateQuiet());

        (account.HasThrottleHint, account.ThrottleHintText).ShouldBe((false, string.Empty));
    }

    [Fact]
    public void when_the_hint_is_shown_then_the_sync_status_is_unchanged()
    {
        var account = CreateViewModel(ThrottleHintFactory.CreateSuggestLowerConcurrency());

        (account.Status.IsSyncing, account.HasFailure, account.NeedsSignIn).ShouldBe((false, false, false));
    }

    private static AccountViewModel CreateViewModel(ThrottleHint hint) => new(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>()).WithThrottleHint(hint), [], TestData.CreateLocalization(), TestData.CreateTime());
}
