using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAnAccountUnionFactory
{
    [Fact]
    public void when_each_removal_failure_is_created_then_it_is_that_case_with_its_payload()
    {
        var store = AccountRemovalFailureFactory.CreateStore(new ErrorResponse("locked"));
        var provider = AccountRemovalFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient());

        (AccountRemovalFailureFactory.CreateNotFound() is AccountRemovalFailure.NotFound, AccountRemovalFailureFactory.CreateSyncStillRunning() is AccountRemovalFailure.SyncStillRunning, store.ShouldBeOfType<AccountRemovalFailure.Store>().Error.Message, provider.ShouldBeOfType<AccountRemovalFailure.Provider>().Error).ShouldBe((true, true, "locked", ProviderErrorFactory.CreateTransient()));
    }

    [Fact]
    public void when_each_sign_in_state_is_created_then_it_is_that_case()
    {
        var states = new object[] { AccountSignInStateFactory.CreateSignedIn(), AccountSignInStateFactory.CreateSignInRequired(), ReauthFailureFactory.CreateSyncStillRunning() };

        states.Select(state => state.GetType().Name).ShouldBe(["SignedIn", "SignInRequired", "SyncStillRunning"]);
    }
}
