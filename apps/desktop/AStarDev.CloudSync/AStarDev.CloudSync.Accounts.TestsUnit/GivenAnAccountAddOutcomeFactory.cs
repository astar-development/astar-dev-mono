using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

public class GivenAnAccountAddOutcomeFactory
{
    private static readonly AccountId NewAccountId = AccountIdFactory.Create(Guid.Parse("6f1c2b7e-3d4a-4e5f-8a9b-0c1d2e3f4a5b"));

    [Fact]
    public void when_an_account_is_added_then_the_outcome_carries_its_id()
    {
        var outcome = AccountAddOutcomeFactory.CreateAdded(NewAccountId);

        outcome.ShouldBe(new AccountAddOutcome.Added(NewAccountId));
    }

    [Fact]
    public void when_an_existing_account_is_signed_in_again_then_the_outcome_carries_its_id()
    {
        var outcome = AccountAddOutcomeFactory.CreateReAuthenticated(NewAccountId);

        outcome.ShouldBe(new AccountAddOutcome.ReAuthenticated(NewAccountId));
    }

    [Fact]
    public void when_the_user_cancels_then_the_outcome_is_cancelled()
    {
        var outcome = AccountAddOutcomeFactory.CreateCancelled();

        outcome.ShouldBeOfType<AccountAddOutcome.Cancelled>();
    }

    [Fact]
    public void when_the_attempt_fails_then_the_outcome_carries_the_failure()
    {
        var outcome = AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork());

        outcome.ShouldBe(new AccountAddOutcome.Failed(new AccountAddFailure.Network()));
    }
}
