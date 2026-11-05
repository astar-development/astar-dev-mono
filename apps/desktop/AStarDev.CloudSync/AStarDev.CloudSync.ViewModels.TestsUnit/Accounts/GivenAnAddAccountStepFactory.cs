using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.ViewModels.Accounts;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAddAccountStepFactory
{
    [Fact]
    public void when_each_step_is_created_then_it_is_the_matching_case()
    {
        AddAccountStep[] steps = [AddAccountStepFactory.CreateClosed(), AddAccountStepFactory.CreateSigningIn(), AddAccountStepFactory.CreateAlreadyAdded(), AddAccountStepFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork())];

        steps.Select(step => step.GetType()).ShouldBe([typeof(AddAccountStep.Closed), typeof(AddAccountStep.SigningIn), typeof(AddAccountStep.AlreadyAdded), typeof(AddAccountStep.Failed)]);
    }
}
